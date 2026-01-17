using ailab_super_app.Common.Exceptions;
using ailab_super_app.DTOs.Project;
using ailab_super_app.Helpers;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<ProjectListDto>>> GetProjects([FromQuery] PaginationParams paginationParams)
    {
        try
        {
            var result = await _projectService.GetProjectsAsync(paginationParams);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get projects hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get project by ID (Admin or Project Member)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ProjectDto>> GetProjectById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var project = await _projectService.GetProjectByIdAsync(id, userId);
            return Ok(project);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get project by ID hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's projects
    /// </summary>
    [HttpGet("my-projects")]
    [Authorize]
    public async Task<ActionResult<List<ProjectListDto>>> GetMyProjects([FromQuery] string? roleFilter = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var projects = await _projectService.GetUserProjectsAsync(userId.Value, roleFilter);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get my projects hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get specific user's projects (Admin only)
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ProjectListDto>>> GetProjectsByUserId(Guid userId)
    {
        try
        {
            var projects = await _projectService.GetUserProjectsAsync(userId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get user projects by admin error: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new project (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var project = await _projectService.CreateProjectAsync(dto, userId.Value);
            return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Create project hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update project (Admin or Captain)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ProjectDto>> UpdateProject(Guid id, [FromBody] UpdateProjectDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var project = await _projectService.UpdateProjectAsync(id, dto, userId.Value);
            return Ok(project);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update project hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete project (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            await _projectService.DeleteProjectAsync(id, userId.Value);
            return Ok(new { message = "Proje başarıyla silindi" });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Delete project hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get project members (Admin or Project Member)
    /// </summary>
    [HttpGet("{id}/members")]
    [Authorize]
    public async Task<ActionResult<List<ProjectMemberDto>>> GetProjectMembers(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var members = await _projectService.GetProjectMembersAsync(id, userId);
            return Ok(members);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get project members hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add member to project (Admin or Captain)
    /// </summary>
    [HttpPost("{id}/members")]
    [Authorize]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(Guid id, [FromBody] AddProjectMemberDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var member = await _projectService.AddMemberAsync(id, dto, userId.Value);
            return Ok(member);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Add member hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove member from project (Admin or Captain)
    /// </summary>
    [HttpDelete("{id}/members/{userId}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            await _projectService.RemoveMemberAsync(id, userId, currentUserId.Value);
            return Ok(new { message = "Üye başarıyla kaldırıldı" });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Remove member hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update member role (Admin only)
    /// </summary>
    [HttpPut("{id}/members/{userId}/role")]
    [Authorize]
    public async Task<ActionResult<ProjectMemberDto>> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateProjectMemberRoleDto dto)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });
            }

            var member = await _projectService.UpdateMemberRoleAsync(id, userId, dto, currentUserId.Value);
            return Ok(member);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update member role hatası: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    #region Private Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    #endregion
}
