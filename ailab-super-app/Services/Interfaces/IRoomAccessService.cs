using ailab_super_app.DTOs.Rfid;
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
    /// Register RFID card for a user
    /// </summary>
    Task<RfidCard> RegisterCardAsync(RegisterCardRequestDto request, Guid? registeredBy);
}
