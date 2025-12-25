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
    private readonly IProjectService _projectService;

    public RoomAccessService(
        AppDbContext context, 
        ILogger<RoomAccessService> logger,
        UserManager<User> userManager,
        IProjectService projectService)
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
            // 1. Reader Validation
            var reader = await _context.RfidReaders
                .FirstOrDefaultAsync(r => r.ReaderUid == request.ReaderUid && r.IsActive);

            if (reader == null)
            {
                return new CardScanResponseDto { Success = false, Message = "Geçersiz RFID okuyucu", DoorShouldOpen = false };
            }

            // 2. Card Validation
            var rfidCard = await _context.RfidCards
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CardUid == request.CardUid && c.IsActive && !c.IsDeleted);

            if (rfidCard == null || rfidCard.UserId == null)
            {
                return new CardScanResponseDto { Success = false, Message = "Geçersiz RFID kart", DoorShouldOpen = false };
            }

            var userId = rfidCard.UserId.Value;

            // 3. Mevcut açık oturumu bul (ExitTime == null)
            var activeSession = await _context.LabEntries
                .FirstOrDefaultAsync(le => le.UserId == userId && le.ExitTime == null);

            bool isUserInside = (activeSession != null);
            bool doorShouldOpen = false;
            bool isEntryAction = false; // True: Giriş yapıyor, False: Çıkış yapıyor
            string actionMessage = "";

            // 4. Logic: İçeride mi değil mi ve okuyucu nerede?
            if (!isUserInside && reader.Location == ReaderLocation.Outside)
            {
                // Dışarıdan Giriş
                doorShouldOpen = true;
                isEntryAction = true;
                actionMessage = "Giriş yapıldı";
            }
            else if (!isUserInside && reader.Location == ReaderLocation.Inside)
            {
                // İçeriden okutma (ama sistemde dışarıda görünüyor) -> Giriş sayalım
                doorShouldOpen = false; // Kapı açılmaz
                isEntryAction = true;
                actionMessage = "İçeride konumlandırıldı (Düzeltme)";
            }
            else if (isUserInside && reader.Location == ReaderLocation.Inside)
            {
                // İçeriden Çıkış
                doorShouldOpen = true;
                isEntryAction = false;
                actionMessage = "Çıkış yapıldı";
            }
            else if (isUserInside && reader.Location == ReaderLocation.Outside)
            {
                // Dışarıdan okutma (ama sistemde içeride görünüyor) -> Çıkış sayalım
                doorShouldOpen = false; // Kapı açılmaz
                isEntryAction = false;
                actionMessage = "Çıkış kaydı (Unutulan çıkış düzeltmesi)";
            }

            // 5. Kapı Açma
            if (doorShouldOpen)
            {
                await UpdateDoorStateAsync(reader.RoomId, true);
            }

            // 6. LabEntry Yönetimi
            if (isEntryAction)
            {
                if (activeSession == null) // Zaten içeride değilse yeni kayıt aç
                {
                    var newEntry = new LabEntry
                    {
                        UserId = userId,
                        RoomId = reader.RoomId,
                        RfidCardId = rfidCard.Id,
                        ReaderUid = request.ReaderUid,
                        EntryTime = DateTime.UtcNow,
                        ExitTime = null // Açık oturum
                    };
                    _context.LabEntries.Add(newEntry);
                    
                    // LabCurrentOccupancy güncelle (Performans için tutuyorsak)
                    var existingOccupancy = await _context.LabCurrentOccupancy.FirstOrDefaultAsync(o => o.UserId == userId);
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
            }
            else // Çıkış
            {
                if (activeSession != null)
                {
                    activeSession.ExitTime = DateTime.UtcNow;
                    activeSession.DurationMinutes = (int)(activeSession.ExitTime.Value - activeSession.EntryTime).TotalMinutes;
                    // EntryType güncellemesi YOK.
                    
                    // LabCurrentOccupancy sil
                    var occupancy = await _context.LabCurrentOccupancy.FirstOrDefaultAsync(o => o.UserId == userId);
                    if (occupancy != null)
                    {
                        _context.LabCurrentOccupancy.Remove(occupancy);
                    }
                }
            }

            // 7. Update RfidCard LastUsed
            rfidCard.LastUsed = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CardScanResponseDto
            {
                Success = true,
                Message = actionMessage,
                DoorShouldOpen = doorShouldOpen,
                UserName = rfidCard.User?.FullName ?? rfidCard.User?.UserName ?? "Bilinmeyen",
                IsEntry = isEntryAction
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Card scan error: {Message}", ex.Message);
            return new CardScanResponseDto { Success = false, Message = "Sistem hatası", DoorShouldOpen = false };
        }
    }

    public async Task<DoorStatusResponseDto> GetDoorStatusAsync(Guid roomId)
    {
        var doorState = await _context.DoorStates
            .Include(d => d.Room)
            .FirstOrDefaultAsync(d => d.RoomId == roomId);

        if (doorState == null)
        {
            doorState = new DoorState { Id = Guid.NewGuid(), RoomId = roomId, IsOpen = false, LastUpdatedAt = DateTime.UtcNow };
            _context.DoorStates.Add(doorState);
            await _context.SaveChangesAsync();
        }

        bool currentStatus = doorState.IsOpen;
        
        if (currentStatus)
        {
            doorState.IsOpen = false; // Reset on read
            doorState.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new DoorStatusResponseDto
        {
            RoomId = doorState.RoomId,
            RoomName = doorState.Room?.Name ?? "Bilinmeyen Oda",
            IsOpen = currentStatus,
            LastUpdatedAt = doorState.LastUpdatedAt
        };
    }

    public async Task ResetDoorStatusAsync(Guid roomId)
    {
        await UpdateDoorStateAsync(roomId, false);
    }

    public async Task<RfidCard> RegisterCardAsync(RegisterCardRequestDto request, Guid registeredBy)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted);
        if (user == null) throw new ArgumentException("Kullanıcı bulunamadı");

        var existingCard = await _context.RfidCards.FirstOrDefaultAsync(c => c.CardUid == request.CardUid && !c.IsDeleted);

        if (existingCard != null)
        {
            existingCard.UserId = request.UserId;
            existingCard.RegisteredBy = registeredBy;
            existingCard.RegisteredAt = DateTime.UtcNow;
            existingCard.IsActive = true;
            await _context.SaveChangesAsync();
            return existingCard;
        }
        else
        {
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
            var button = await _context.PhysicalButtons.FirstOrDefaultAsync(b => b.ButtonUid == request.ButtonUid && b.IsActive);
            if (button == null) return new ButtonPressResponseDto { Success = false, Message = "Geçersiz buton", DoorShouldOpen = false };

            await UpdateDoorStateAsync(button.RoomId, true);

            // Log sadece ButtonPressLogs tablosuna, RoomAccesses kaldırıldı.
            _context.ButtonPressLogs.Add(new ButtonPressLog
            {
                Id = Guid.NewGuid(),
                ButtonId = button.Id,
                RoomId = button.RoomId,
                ButtonUid = request.ButtonUid,
                PressedAt = DateTime.UtcNow,
                Success = true
            });

            await _context.SaveChangesAsync();

            return new ButtonPressResponseDto { Success = true, Message = "Kapı açıldı", DoorShouldOpen = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Button press error");
            return new ButtonPressResponseDto { Success = false, Message = "Hata", DoorShouldOpen = false };
        }
    }

    private async Task UpdateDoorStateAsync(Guid roomId, bool isOpen)
    {
        var doorState = await _context.DoorStates.FirstOrDefaultAsync(d => d.RoomId == roomId);
        if (doorState == null)
        {
            doorState = new DoorState { Id = Guid.NewGuid(), RoomId = roomId, IsOpen = isOpen, LastUpdatedAt = DateTime.UtcNow };
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
        var labRoom = await _context.Rooms.FirstOrDefaultAsync();
        if (labRoom == null) throw new Exception("Lab odası bulunamadı.");

        // Occupancy tablosunu kullanmaya devam edebiliriz veya LabEntries'ten ExitTime == null olanları çekebiliriz.
        // Tutarlılık için LabEntries'i kullanmak daha güvenlidir ama Occupancy tablosu performans için varsa onu da kullanabiliriz.
        // Kodda Occupancy tablosunu güncellediğim için oradan çekiyorum.
        
        var currentOccupants = await _context.LabCurrentOccupancy
                                             .Where(o => o.RoomId == labRoom.Id)
                                             .Include(o => o.User)
                                             .ToListAsync();

        var peopleInside = currentOccupants
            .Select(o => o.User.FullName ?? o.User.UserName ?? "Bilinmeyen")
            .ToList();

        var totalCapacity = await _context.RfidCards.Where(rc => rc.IsActive && !rc.IsDeleted).CountAsync();

        return new GlobalLabStatusDto
        {
            CurrentOccupancyCount = currentOccupants.Count,
            TotalCapacity = totalCapacity,
            PeopleInside = peopleInside
        };
    }

    public async Task<UserLabStatsDto> GetUserLabStatsAsync(Guid userId)
    {
        // Son giriş: ExitTime'a bakmaksızın en son EntryTime
        var lastEntryTime = await _context.LabEntries
                                      .Where(le => le.UserId == userId)
                                      .OrderByDescending(le => le.EntryTime)
                                      .Select(le => (DateTime?)le.EntryTime)
                                      .FirstOrDefaultAsync();

        // Toplam süre: Tamamlanmışlar + Şu anki aktif oturum
        var finishedDuration = await _context.LabEntries
                                             .Where(le => le.UserId == userId && le.DurationMinutes.HasValue)
                                             .SumAsync(le => le.DurationMinutes!.Value);

        var activeSession = await _context.LabEntries
                                          .FirstOrDefaultAsync(le => le.UserId == userId && le.ExitTime == null);
        
        if (activeSession != null)
        {
            var currentDuration = (DateTime.UtcNow - activeSession.EntryTime).TotalMinutes;
            finishedDuration += (int)currentDuration;
        }

        return new UserLabStatsDto
        {
            LastEntryDate = lastEntryTime,
            TotalTimeSpent = TimeSpan.FromMinutes(finishedDuration)
        };
    }

    public async Task<TeammateLabStatusDto> GetTeammateLabStatusAsync(Guid userId)
    {
        var userProjectIds = await _context.ProjectMembers
                                            .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                                            .Select(pm => pm.ProjectId)
                                            .ToListAsync();

        if (!userProjectIds.Any()) return new TeammateLabStatusDto { TeammatesInsideCount = 0, TotalTeammatesCount = 0 };

        var allTeammateUserIds = await _context.ProjectMembers
                                                .Where(pm => userProjectIds.Contains(pm.ProjectId) && pm.UserId != userId && !pm.IsDeleted)
                                                .Select(pm => pm.UserId)
                                                .Distinct()
                                                .ToListAsync();

        if (!allTeammateUserIds.Any()) return new TeammateLabStatusDto { TeammatesInsideCount = 0, TotalTeammatesCount = 0 };

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
