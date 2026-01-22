using ailab_super_app.DTOs.Rfid;
using ailab_super_app.DTOs.Room;
using ailab_super_app.DTOs.Statistics;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Tüm istatistik endpointleri yetkilendirme gerektirir
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
        [AllowAnonymous] // IoT cihazları için yetkilendirme yok
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
        /// Process physical button press (called by ESP button hardware)
        /// </summary>
        [HttpPost("button-press")]
        [AllowAnonymous] // IoT cihazları için yetkilendirme yok
        public async Task<ActionResult<ButtonPressResponseDto>> ProcessButtonPress([FromBody] ButtonPressRequestDto request)
        {
            try
            {
                var result = await _roomAccessService.ProcessButtonPressAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Button press processing error: {Message}", ex.Message);
                return BadRequest(new ButtonPressResponseDto
                {
                    Success = false,
                    Message = "Buton işlemi başarısız",
                    DoorShouldOpen = false
                });
            }
        }

        /// <summary>
        /// Get door status for a room (called by door controller)
        /// </summary>
        [HttpGet("{roomId}/door-status")]
        [AllowAnonymous] // IoT cihazları için yetkilendirme yok
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
        /// Register RFID card for a user
        /// </summary>
        [HttpPost("register-card")]
        [Authorize] // Sadece yetkili kullanıcılar (örn: Admin)
        public async Task<ActionResult> RegisterCard([FromBody] RegisterCardRequestDto request)
        {
            try
            {
                // Get current user's ID from JWT token
                var currentUserId = GetCurrentUserId();
                var rfidCard = await _roomAccessService.RegisterCardAsync(request, currentUserId);
                
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

            /// <summary>
            /// Get global lab occupancy and capacity statistics.
            /// </summary>
            [HttpGet("stats/global")]
            public async Task<ActionResult<GlobalLabStatusDto>> GetGlobalLabStats()
            {
                try
                {
                    var stats = await _roomAccessService.GetGlobalLabStatusAsync();
                    return Ok(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Get global lab stats error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }
        
            /// <summary>
            /// Get lab usage statistics for the current user.
            /// </summary>
            [HttpGet("stats/me")]
            public async Task<ActionResult<UserLabStatsDto>> GetMyLabStats()
            {
                try
                {
                    var userId = GetCurrentUserId();
                    var stats = await _roomAccessService.GetUserLabStatsAsync(userId);
                    return Ok(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Get user lab stats error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }

            /// <summary>
            /// Get lab usage statistics for a specific user (Admin only).
            /// </summary>
            [HttpGet("stats/user/{userId}")]
            [Authorize(Roles = "Admin")]
            public async Task<ActionResult<UserLabStatsDto>> GetUserLabStats(Guid userId)
            {
                try
                {
                    var stats = await _roomAccessService.GetUserLabStatsAsync(userId);
                    return Ok(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Get user lab stats by admin error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }
        
            /// <summary>
            /// Get lab occupancy statistics for current user's teammates.
            /// </summary>
            [HttpGet("stats/teammates")]
            public async Task<ActionResult<TeammateLabStatusDto>> GetTeammateLabStats()
            {
                try
                {
                    var userId = GetCurrentUserId();
                    var stats = await _roomAccessService.GetTeammateLabStatusAsync(userId);
                    return Ok(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Get teammate lab stats error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }
            private Guid GetCurrentUserId()
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    throw new UnauthorizedAccessException("Kullanıcı kimliği doğrulanamadı.");
                }
                return userId;
            }
        
            /// <summary>
            /// Force checkout user(s) from lab (Admin only)
            /// </summary>
            [HttpPost("force-checkout")]
            [Authorize(Roles = "Admin")]
            public async Task<IActionResult> ForceCheckout([FromBody] ForceCheckoutDto dto)
            {
                try
                {
                    var adminId = GetCurrentUserId();
                    var count = await _roomAccessService.ForceCheckoutAsync(dto, adminId);
                    return Ok(new { message = $"{count} kişi başarıyla çıkarıldı." });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Force checkout error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }

            /// <summary>
            /// Update room access mode (Admin only)
            /// </summary>
            [HttpPut("{roomId}/access-mode")]
            [Authorize(Roles = "Admin")]
            public async Task<IActionResult> UpdateAccessMode(Guid roomId, [FromBody] UpdateRoomAccessModeDto dto)
            {
                try
                {
                    await _roomAccessService.UpdateRoomAccessModeAsync(roomId, dto.Mode);
                    return Ok(new { message = "Erişim modu güncellendi", mode = dto.Mode });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Update access mode error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }

            /// <summary>
            /// Get room access mode
            /// </summary>
            [HttpGet("{roomId}/access-mode")]
            public async Task<ActionResult<RoomAccessMode>> GetAccessMode(Guid roomId)
            {
                try
                {
                    var mode = await _roomAccessService.GetRoomAccessModeAsync(roomId);
                    return Ok(new { mode });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Get access mode error: {ex.Message}");
                    return BadRequest(new { message = ex.Message });
                }
            }
        }}