using ailab_super_app.DTOs.Rfid;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomAccessService _roomAccessService;
        private readonly ILogger<RoomsController> _logger;

        public RoomsController(IRoomAccessService roomAccessService, ILogger<RoomsController> logger)
        {
            _roomAccessService = roomAccessService;
            _logger = logger;
        }

        /// <summary>
        /// Process RFID card scan (called by RFID hardware)
        /// </summary>
        [HttpPost("card-scan")]
        public async Task<ActionResult<CardScanResponseDto>> ProcessCardScan([FromBody] CardScanRequestDto request)
        {
            try
            {
                var result = await _roomAccessService.ProcessCardScanAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card scan processing error: {Message}", ex.Message);
                return BadRequest(new CardScanResponseDto
                {
                    Success = false,
                    Message = "Kart okuma işlemi başarısız",
                    DoorShouldOpen = false
                });
            }
        }

        /// <summary>
        /// Get door status for a room (called by door controller)
        /// </summary>
        [HttpGet("{roomId}/door-status")]
        public async Task<ActionResult<DoorStatusResponseDto>> GetDoorStatus(Guid roomId)
        {
            try
            {
                var result = await _roomAccessService.GetDoorStatusAsync(roomId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get door status error: {Message}", ex.Message);
                return BadRequest(new { message = "Kapı durumu alınamadı" });
            }
        }

        /// <summary>
        /// Register RFID card for a user (admin only)
        /// </summary>
        [HttpPost("register-card")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RegisterCard([FromBody] RegisterCardRequestDto request)
        {
            try
            {
                // Get current user's ID from JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var registeredBy))
                {
                    return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
                }

                var rfidCard = await _roomAccessService.RegisterCardAsync(request, registeredBy);
                
                return Ok(new { 
                    message = "RFID kart başarıyla kaydedildi",
                    cardId = rfidCard.Id,
                    cardUid = rfidCard.CardUid,
                    userId = rfidCard.UserId
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Card registration validation error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card registration error: {Message}", ex.Message);
                return BadRequest(new { message = "Kart kaydı başarısız" });
            }
        }
    }
}
