using ailab_super_app.DTOs.User;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUserService userService, 
            IProfileService profileService,
            ILogger<ProfileController> logger)
        {
            _userService = userService;
            _profileService = profileService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetMyProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetUserByIdAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get profile error: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update current user's profile info (Phone, etc.)
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<UserDto>> UpdateMyProfile([FromBody] UpdateUserDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.UpdateUserAsync(userId, dto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update profile error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update profile image URL (After frontend upload to Firebase)
        /// </summary>
        [HttpPut("image")]
        public async Task<IActionResult> UpdateProfileImage([FromBody] UpdateProfileImageDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _profileService.UpdateProfileImageAsync(userId, dto);
                return Ok(new { message = "Profil fotoğrafı başarıyla güncellendi.", url = dto.ProfileImageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update profile image error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get list of default system avatars
        /// </summary>
        [HttpGet("avatars/defaults")]
        public ActionResult<DefaultAvatarListDto> GetDefaultAvatars()
        {
            var avatars = _profileService.GetDefaultAvatars();
            return Ok(avatars);
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
    }
}