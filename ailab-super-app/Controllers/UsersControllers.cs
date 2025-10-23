using ailab_super_app.DTOs.User;
using ailab_super_app.Helpers;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with pagination (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserListDto>>> GetUsers([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var result = await _userService.GetUsersAsync(paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get users hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get user by ID hatası: {ex.Message}");
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update user information (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var user = await _userService.UpdateUserAsync(id, dto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update user hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update user status (Admin only)
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<UserDto>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
        {
            try
            {
                var user = await _userService.UpdateUserStatusAsync(id, dto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update user status hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete user (soft delete, Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                // Get current user's ID from JWT token
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var deletedBy))
                {
                    return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
                }

                await _userService.DeleteUserAsync(id, deletedBy);
                return Ok(new { message = "Kullanıcı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete user hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
