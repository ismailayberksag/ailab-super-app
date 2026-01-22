using ailab_super_app.DTOs.Rfid;
using ailab_super_app.DTOs.Statistics;
using ailab_super_app.Models.Enums;
using ailab_super_app.Models;

namespace ailab_super_app.Services.Interfaces;

public interface IRoomAccessService
{
    /// <summary>
    /// Process RFID card scan and determine door action
    /// </summary>
    Task<CardScanResponseDto> ProcessCardScanAsync(CardScanRequestDto request);
    
    /// <summary>
    /// Get current door status for a room
    /// </summary>
    Task<DoorStatusResponseDto> GetDoorStatusAsync(Guid roomId);
    
    /// <summary>
    /// Reset door status to closed for a room
    /// </summary>
    Task ResetDoorStatusAsync(Guid roomId);
    
    /// <summary>
    /// Register RFID card for a user (admin only)
    /// </summary>
    Task<RfidCard> RegisterCardAsync(RegisterCardRequestDto request, Guid registeredBy);
    
    /// <summary>
    /// Process physical button press and open door (no entry/exit logging)
    /// </summary>
    Task<ButtonPressResponseDto> ProcessButtonPressAsync(ButtonPressRequestDto request);

    Task<GlobalLabStatusDto> GetGlobalLabStatusAsync();
    Task<UserLabStatsDto> GetUserLabStatsAsync(Guid userId);
    Task<TeammateLabStatusDto> GetTeammateLabStatusAsync(Guid userId);
    
    // Admin Operations
    Task<int> ForceCheckoutAsync(ForceCheckoutDto dto, Guid adminId);
    
    // Access Mode Management
    Task UpdateRoomAccessModeAsync(Guid roomId, RoomAccessMode mode);
    Task<RoomAccessMode> GetRoomAccessModeAsync(Guid roomId);
}
