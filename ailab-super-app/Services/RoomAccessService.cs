using ailab_super_app.Data;
using ailab_super_app.DTOs.Rfid;
using ailab_super_app.DTOs.Statistics;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ailab_super_app.Services;

public class RoomAccessService : IRoomAccessService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoomAccessService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IProjectService _projectService; // Takım arkadaşları için

    public RoomAccessService(
        AppDbContext context, 
        ILogger<RoomAccessService> logger,
        UserManager<User> userManager,
        IProjectService projectService) // Inject edildi
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _projectService = projectService;
    }

    public async Task<CardScanResponseDto> ProcessCardScanAsync(CardScanRequestDto request)
    {
        try
        {
            // 1. Validate readerUid exists in rfid_readers table
            var reader = await _context.RfidReaders
                .FirstOrDefaultAsync(r => r.ReaderUid == request.ReaderUid && r.IsActive);

            if (reader == null)
            {
                return new CardScanResponseDto
                {
                    Success = false,
                    Message = "Geçersiz RFID okuyucu",
                    DoorShouldOpen = false
                };
            }

            // 2. Validate cardUid exists in rfid_cards table and get associated UserId
            var rfidCard = await _context.RfidCards
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CardUid == request.CardUid && c.IsActive && !c.IsDeleted);

            if (rfidCard == null || rfidCard.UserId == null)
            {
                return new CardScanResponseDto
                {
                    Success = false,
                    Message = "Geçersiz RFID kart",
                    DoorShouldOpen = false
                };
            }

            var userId = rfidCard.UserId.Value;

            // 3. Check if user is currently in lab (exists in lab_current_occupancy)
            var existingOccupancy = await _context.LabCurrentOccupancy
                .FirstOrDefaultAsync(o => o.UserId == userId && o.RoomId == reader.RoomId); // RoomId'ye göre kontrol

            bool isUserCurrentlyInThisRoom = (existingOccupancy != null);

            // 4. Apply door logic based on table
            bool doorShouldOpen = false;
            EntryType accessType;
            string actionMessage = "";

            if (!isUserCurrentlyInThisRoom && reader.Location == ReaderLocation.Outside) // Dışarıdan Giriş
            {
                doorShouldOpen = true;
                accessType = EntryType.Entry;
                actionMessage = "Giriş yapıldı";
            }
            else if (!isUserCurrentlyInThisRoom && reader.Location == ReaderLocation.Inside) // İçeriden konumlandırma (genelde olmaz ama log için)
            {
                doorShouldOpen = false; // Kapı açılmaz
                accessType = EntryType.Entry;
                actionMessage = "İçeride konumlandırıldı";
            }
            else if (isUserCurrentlyInThisRoom && reader.Location == ReaderLocation.Inside) // İçeriden Çıkış
            {
                doorShouldOpen = true; // Kapı açılır
                accessType = EntryType.Exit;
                actionMessage = "Çıkış yapıldı";
            }
            else if (isUserCurrentlyInThisRoom && reader.Location == ReaderLocation.Outside) // Dışarıdan çıkış (yanlış okuma veya unutma durumu)
            {
                doorShouldOpen = false; // Kapı açılmaz
                accessType = EntryType.Exit;
                actionMessage = "Çıkış kaydı (unutulan çıkış)";
            }
            else
            {
                // Bilinmeyen durum, güvenlik için kapı açma
                return new CardScanResponseDto
                {
                    Success = false,
                    Message = "Erişim hatası: Bilinmeyen durum",
                    DoorShouldOpen = false
                };
            }


            // 5. Update DoorState.IsOpen if door should open
            if (doorShouldOpen)
            {
                // Sadece TRUE yapıyoruz, kapatma işlemini GetDoorStatusAsync'e bıraktık.
                await UpdateDoorStateAsync(reader.RoomId, true);
            }

            // 6. Update LabCurrentOccupancy
            if (accessType == EntryType.Entry)
            {
                if (existingOccupancy == null)
                {
                    _context.LabCurrentOccupancy.Add(new LabCurrentOccupancy
                    {
                        UserId = userId,
                        RoomId = reader.RoomId,
                        EntryTime = DateTime.UtcNow,
                        CardUid = request.CardUid
                    });
                }
            }
            else // Exit
            {
                if (existingOccupancy != null)
                {
                    _context.LabCurrentOccupancy.Remove(existingOccupancy);
                }
            }

            // 7. Create RoomAccess log entry
            var roomAccess = new RoomAccess
            {
                RoomId = reader.RoomId,
                RfidCardId = rfidCard.Id,
                UserId = userId,
                Direction = accessType,
                IsAuthorized = true, // Yetkilendirme yapıldı varsayımıyla
                AccessedAt = DateTime.UtcNow,
                RawPayload = $"CardUid: {request.CardUid}, ReaderUid: {request.ReaderUid}"
            };

            _context.RoomAccesses.Add(roomAccess);

            // 8. Create or Update LabEntry record
            if (accessType == EntryType.Entry)
            {
                var newLabEntry = new LabEntry
                {
                    UserId = userId,
                    RoomId = reader.RoomId,
                    RfidCardId = rfidCard.Id,
                    EntryType = EntryType.Entry,
                    EntryTime = DateTime.UtcNow
                };
                _context.LabEntries.Add(newLabEntry);
            }
            else // Exit
            {
                // Kullanıcının en son açık LabEntry kaydını bul ve kapat
                var lastEntry = await _context.LabEntries
                                            .Where(le => le.UserId == userId && le.ExitTime == null && le.RoomId == reader.RoomId)
                                            .OrderByDescending(le => le.EntryTime)
                                            .FirstOrDefaultAsync();
                if (lastEntry != null)
                {
                    lastEntry.ExitTime = DateTime.UtcNow;
                    lastEntry.EntryType = EntryType.Exit; // Aslında çıkış ama log için EntryType'ı Exit de tutabiliriz
                    lastEntry.DurationMinutes = (int)(lastEntry.ExitTime.Value - lastEntry.EntryTime).TotalMinutes;
                }
            }

            // 9. Update RfidCard.LastUsed
            rfidCard.LastUsed = DateTime.UtcNow;

            // 10. Save all changes
            await _context.SaveChangesAsync();

            return new CardScanResponseDto
            {
                Success = true,
                Message = actionMessage,
                DoorShouldOpen = doorShouldOpen,
                UserName = rfidCard.User?.FullName ?? rfidCard.User?.UserName ?? "Bilinmeyen",
                IsEntry = (accessType == EntryType.Entry)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Card scan processing error: {Message}", ex.Message);
            return new CardScanResponseDto
            {
                Success = false,
                Message = "Sistem hatası oluştu",
                DoorShouldOpen = false
            };
        }
    }

    public async Task<DoorStatusResponseDto> GetDoorStatusAsync(Guid roomId)
    {
        var doorState = await _context.DoorStates
            .Include(d => d.Room)
            .FirstOrDefaultAsync(d => d.RoomId == roomId);

        if (doorState == null)
        {
            // Create default door state if it doesn't exist
            doorState = new DoorState
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                IsOpen = false,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.DoorStates.Add(doorState);
            await _context.SaveChangesAsync();
        }

        // DEĞİŞİKLİK: Consume-on-read mantığı
        // Mevcut durumu al
        bool currentStatus = doorState.IsOpen;
        DateTime lastUpdate = doorState.LastUpdatedAt;

        // Eğer kapı durumu AÇIK (True) ise, okunduğu anda KAPALI (False) olarak güncelle
        if (currentStatus)
        {
            doorState.IsOpen = false;
            doorState.LastUpdatedAt = DateTime.UtcNow;
            
            // Veritabanını güncelle ki bir sonraki istekte false dönsün
            await _context.SaveChangesAsync();
        }

        return new DoorStatusResponseDto
        {
            RoomId = doorState.RoomId,
            RoomName = doorState.Room?.Name ?? "Bilinmeyen Oda",
            IsOpen = currentStatus, // Burada API'ye hala TRUE dönüyoruz (ESP kapıyı açsın diye)
            LastUpdatedAt = lastUpdate
        };
    }

    public async Task ResetDoorStatusAsync(Guid roomId)
    {
        await UpdateDoorStateAsync(roomId, false);
    }

    public async Task<RfidCard> RegisterCardAsync(RegisterCardRequestDto request, Guid registeredBy)
    {
        // Validate user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted);

        if (user == null)
        {
            throw new ArgumentException("Kullanıcı bulunamadı");
        }

        // Check if card already exists
        var existingCard = await _context.RfidCards
            .FirstOrDefaultAsync(c => c.CardUid == request.CardUid && !c.IsDeleted);

        if (existingCard != null)
        {
            // Update existing card
            existingCard.UserId = request.UserId;
            existingCard.RegisteredBy = registeredBy;
            existingCard.RegisteredAt = DateTime.UtcNow;
            existingCard.IsActive = true;
            existingCard.IsDeleted = false;
            existingCard.DeletedAt = null;
            existingCard.DeletedBy = null;

            await _context.SaveChangesAsync();
            return existingCard;
        }
        else
        {
            // Create new card
            var newCard = new RfidCard
            {
                Id = Guid.NewGuid(),
                CardUid = request.CardUid,
                UserId = request.UserId,
                IsActive = true,
                RegisteredBy = registeredBy,
                RegisteredAt = DateTime.UtcNow
            };

            _context.RfidCards.Add(newCard);
            await _context.SaveChangesAsync();
            return newCard;
        }
    }

    public async Task<ButtonPressResponseDto> ProcessButtonPressAsync(ButtonPressRequestDto request)
    {
        try
        {
            // 1. Validate button exists and is active
            var button = await _context.PhysicalButtons
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.ButtonUid == request.ButtonUid && b.IsActive);

            if (button == null)
            {
                return new ButtonPressResponseDto
                {
                    Success = false,
                    Message = "Geçersiz veya pasif buton",
                    DoorShouldOpen = false
                };
            }

            var roomId = button.RoomId;

            // 2. Update DoorState.IsOpen to true (ESP will poll and open door)
            await UpdateDoorStateAsync(roomId, true);

            // 3. Create ButtonPressLog entry
            var buttonPressLog = new ButtonPressLog
            {
                Id = Guid.NewGuid(),
                ButtonId = button.Id,
                RoomId = roomId,
                ButtonUid = request.ButtonUid,
                PressedAt = DateTime.UtcNow,
                Success = true
            };

            _context.ButtonPressLogs.Add(buttonPressLog);

            // 4. Save changes
            await _context.SaveChangesAsync();

            // 5. DEĞİŞİKLİK: Manuel resetleme kaldırıldı.
            // GetDoorStatusAsync çağrıldığında resetlenecek.

            return new ButtonPressResponseDto
            {
                Success = true,
                Message = "Kapı açma komutu gönderildi",
                DoorShouldOpen = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Button press processing error: {Message}", ex.Message);
            
            // Try to log the error if button exists
            try
            {
                var button = await _context.PhysicalButtons
                    .FirstOrDefaultAsync(b => b.ButtonUid == request.ButtonUid);
                
                if (button != null)
                {
                    var errorLog = new ButtonPressLog
                    {
                        Id = Guid.NewGuid(),
                        ButtonId = button.Id,
                        RoomId = button.RoomId,
                        ButtonUid = request.ButtonUid,
                        PressedAt = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                    _context.ButtonPressLogs.Add(errorLog);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore logging errors
            }

            return new ButtonPressResponseDto
            {
                Success = false,
                Message = "Sistem hatası oluştu",
                DoorShouldOpen = false
            };
        }
    }

    private async Task UpdateDoorStateAsync(Guid roomId, bool isOpen)
    {
        var doorState = await _context.DoorStates
            .FirstOrDefaultAsync(d => d.RoomId == roomId);

        if (doorState == null)
        {
            doorState = new DoorState
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                IsOpen = isOpen,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.DoorStates.Add(doorState);
        }
        else
        {
            doorState.IsOpen = isOpen;
            doorState.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<GlobalLabStatusDto> GetGlobalLabStatusAsync()
    {
        var labRoom = await _context.Rooms.FirstOrDefaultAsync(); // Varsayım: Tek bir Lab odası var
        if (labRoom == null) throw new Exception("Lab odası bulunamadı.");

        // 1. İçerideki kişiler ve bilgileri
        var currentOccupants = await _context.LabCurrentOccupancy
                                             .Where(o => o.RoomId == labRoom.Id)
                                             .Include(o => o.User) // Kullanıcı bilgilerini çek
                                             .ToListAsync();

        var peopleInside = currentOccupants
            .Select(o => o.User.FullName ?? o.User.UserName ?? "Bilinmeyen Kullanıcı")
            .ToList();

        // 2. Toplam kapasite (aktif RFID kart sayısı)
        var totalCapacity = await _context.RfidCards
                                          .Where(rc => rc.IsActive && !rc.IsDeleted)
                                          .CountAsync();

        return new GlobalLabStatusDto
        {
            CurrentOccupancyCount = currentOccupants.Count,
            TotalCapacity = totalCapacity,
            PeopleInside = peopleInside
        };
    }

    public async Task<UserLabStatsDto> GetUserLabStatsAsync(Guid userId)
    {
        // Son giriş tarihini bul
        var lastEntry = await _context.LabEntries
                                      .Where(le => le.UserId == userId && le.EntryType == EntryType.Entry)
                                      .OrderByDescending(le => le.EntryTime)
                                      .Select(le => (DateTime?)le.EntryTime) // Nullable olarak seç
                                      .FirstOrDefaultAsync();

        // Toplam geçirilen süreyi hesapla
        var totalDurationMinutes = await _context.LabEntries
                                                 .Where(le => le.UserId == userId && le.DurationMinutes.HasValue)
                                                 .SumAsync(le => le.DurationMinutes!.Value);

        // Şu an içerideyse, bu süreyi de ekle
        var currentOccupancy = await _context.LabCurrentOccupancy
                                             .FirstOrDefaultAsync(o => o.UserId == userId);

        if (currentOccupancy != null)
        {
            var timeSinceEntry = DateTime.UtcNow - currentOccupancy.EntryTime;
            totalDurationMinutes += (int)timeSinceEntry.TotalMinutes;
        }

        return new UserLabStatsDto
        {
            LastEntryDate = lastEntry,
            TotalTimeSpent = TimeSpan.FromMinutes(totalDurationMinutes)
        };
    }

    public async Task<TeammateLabStatusDto> GetTeammateLabStatusAsync(Guid userId)
    {
        // 1. Kullanıcının dahil olduğu tüm projelerin ID'lerini bul
        var userProjectIds = await _context.ProjectMembers
                                            .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                                            .Select(pm => pm.ProjectId)
                                            .ToListAsync();

        if (!userProjectIds.Any())
        {
            return new TeammateLabStatusDto { TeammatesInsideCount = 0, TotalTeammatesCount = 0 };
        }

        // 2. Bu projelerdeki tüm tekil üyeleri (kendisi hariç) bul
        var allTeammateUserIds = await _context.ProjectMembers
                                                .Where(pm => userProjectIds.Contains(pm.ProjectId) &&
                                                             pm.UserId != userId &&
                                                             !pm.IsDeleted)
                                                .Select(pm => pm.UserId)
                                                .Distinct() // Aynı kişi birden fazla projede olsa bile 1 kez say
                                                .ToListAsync();

        if (!allTeammateUserIds.Any())
        {
            return new TeammateLabStatusDto { TeammatesInsideCount = 0, TotalTeammatesCount = 0 };
        }

        // 3. Labda olan takım arkadaşı sayısını bul
        // Not: LabCurrentOccupancy tablosunda UserId varsa kişi içeridedir.
        var teammatesInsideCount = await _context.LabCurrentOccupancy
                                                  .Where(loc => allTeammateUserIds.Contains(loc.UserId))
                                                  .CountAsync();

        return new TeammateLabStatusDto
        {
            TeammatesInsideCount = teammatesInsideCount,
            TotalTeammatesCount = allTeammateUserIds.Count
        };
    }
}