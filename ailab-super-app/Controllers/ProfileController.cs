using ailab_super_app.DTOs.User;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Sadece giriş yapmış kullanıcılar (herhangi bir rol)
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile (Any authenticated user)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var user = await _userService.GetUserByIdAsync(currentUserId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get my profile hatası: {ex.Message}");
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update current user's profile (Any authenticated user)
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<UserDto>> UpdateMyProfile([FromBody] UpdateUserDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var user = await _userService.UpdateUserAsync(currentUserId, dto);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update my profile hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }
}

