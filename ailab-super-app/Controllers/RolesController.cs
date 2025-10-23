using ailab_super_app.DTOs.Role;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ailab_super_app.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAllRoles()
    {
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get all roles hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get role by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDto>> GetRoleById(Guid id)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get role by ID hatası: {ex.Message}");
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get role by name (Admin only)
    /// </summary>
    [HttpGet("name/{roleName}")]
    public async Task<ActionResult<RoleDto>> GetRoleByName(string roleName)
    {
        try
        {
            var role = await _roleService.GetRoleByNameAsync(roleName);
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get role by name hatası: {ex.Message}");
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new role (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        try
        {
            var role = await _roleService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, role);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Create role hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update role (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var role = await _roleService.UpdateRoleAsync(id, dto);
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update role hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete role (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        try
        {
            await _roleService.DeleteRoleAsync(id);
            return Ok(new { message = "Rol başarıyla silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Delete role hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign role to user (Admin only)
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto dto)
    {
        try
        {
            await _roleService.AssignRoleToUserAsync(dto);
            return Ok(new { message = "Rol başarıyla atandı" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Assign role hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove role from user (Admin only)
    /// </summary>
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto dto)
    {
        try
        {
            await _roleService.RemoveRoleFromUserAsync(dto.UserId, dto.RoleName);
            return Ok(new { message = "Rol başarıyla kaldırıldı" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Remove role hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user's roles (Admin only)
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<string>>> GetUserRoles(Guid userId)
    {
        try
        {
            var roles = await _roleService.GetUserRolesAsync(userId);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get user roles hatası: {ex.Message}");
            return NotFound(new { message = ex.Message });
        }
    }
}

