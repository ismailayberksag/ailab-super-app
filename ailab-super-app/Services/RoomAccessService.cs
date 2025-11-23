using ailab_super_app.Data;
using ailab_super_app.DTOs.Rfid;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services;

public class RoomAccessService : IRoomAccessService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoomAccessService> _logger;

    public RoomAccessService(AppDbContext context, ILogger<RoomAccessService> logger)
    {
        _context = context;
        _logger = logger;
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
            var isUserInLab = await _context.LabCurrentOccupancy
                .AnyAsync(o => o.UserId == userId);

            // 4. Apply door logic based on table
            bool doorShouldOpen = false;
            bool isEntry = false;
            string actionMessage = "";

            if (!isUserInLab && reader.Location == ReaderLocation.Outside)
            {
                // User outside + Outside reader → open door, add to occupancy
                doorShouldOpen = true;
                isEntry = true;
                actionMessage = "Giriş yapıldı";
            }
            else if (!isUserInLab && reader.Location == ReaderLocation.Inside)
            {
                // User outside + Inside reader → don't open, add to occupancy
                doorShouldOpen = false;
                isEntry = true;
                actionMessage = "İçeride konumlandırıldı";
            }
            else if (isUserInLab && reader.Location == ReaderLocation.Outside)
            {
                // User inside + Outside reader → don't open, remove from occupancy
                doorShouldOpen = false;
                isEntry = false;
                actionMessage = "Çıkış yapıldı";
            }
            else if (isUserInLab && reader.Location == ReaderLocation.Inside)
            {
                // User inside + Inside reader → open door, remove from occupancy
                doorShouldOpen = true;
                isEntry = false;
                actionMessage = "Çıkış yapıldı";
            }

            // 5. Update DoorState.IsOpen if door should open
            if (doorShouldOpen)
            {
                await UpdateDoorStateAsync(reader.RoomId, true);
            }

            // 6. Update user occupancy
            if (isEntry)
            {
                // Add user to lab
                var existingOccupancy = await _context.LabCurrentOccupancy
                    .FirstOrDefaultAsync(o => o.UserId == userId);
                
                if (existingOccupancy == null)
                {
                    _context.LabCurrentOccupancy.Add(new LabCurrentOccupancy
                    {
                        UserId = userId,
                        EntryTime = DateTime.UtcNow,
                        CardUid = request.CardUid
                    });
                }
            }
            else
            {
                // Remove user from lab
                var occupancy = await _context.LabCurrentOccupancy
                    .FirstOrDefaultAsync(o => o.UserId == userId);
                
                if (occupancy != null)
                {
                    _context.LabCurrentOccupancy.Remove(occupancy);
                }
            }

            // 7. Create RoomAccess log entry
            var roomAccess = new RoomAccess
            {
                RoomId = reader.RoomId,
                RfidCardId = rfidCard.Id,
                UserId = userId,
                Direction = isEntry ? EntryType.Entry : EntryType.Exit,
                IsAuthorized = true,
                AccessedAt = DateTime.UtcNow,
                RawPayload = $"CardUid: {request.CardUid}, ReaderUid: {request.ReaderUid}"
            };

            _context.RoomAccesses.Add(roomAccess);

            // 8. Create LabEntry record
            var labEntry = new LabEntry
            {
                UserId = userId,
                CardUid = request.CardUid,
                EntryType = isEntry ? EntryType.Entry : EntryType.Exit,
                EntryTime = DateTime.UtcNow
            };

            _context.LabEntries.Add(labEntry);

            // 9. Update RfidCard.LastUsed
            rfidCard.LastUsed = DateTime.UtcNow;

            // 10. Save all changes
            await _context.SaveChangesAsync();

            // 11. IMPORTANT: If door was opened, immediately reset it back to false
            if (doorShouldOpen)
            {
                await UpdateDoorStateAsync(reader.RoomId, false);
            }

            return new CardScanResponseDto
            {
                Success = true,
                Message = actionMessage,
                DoorShouldOpen = doorShouldOpen,
                UserName = rfidCard.User?.FullName ?? rfidCard.User?.UserName ?? "Bilinmeyen",
                IsEntry = isEntry
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

        return new DoorStatusResponseDto
        {
            RoomId = doorState.RoomId,
            RoomName = doorState.Room?.Name ?? "Bilinmeyen Oda",
            IsOpen = doorState.IsOpen,
            LastUpdatedAt = doorState.LastUpdatedAt
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

            // 5. IMPORTANT: Immediately reset door state back to false (ESP will close door after opening)
            await UpdateDoorStateAsync(roomId, false);

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
}
