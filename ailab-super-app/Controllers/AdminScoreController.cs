using ailab_super_app.DTOs.AdminScore;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminScoreController : ControllerBase
{
    private readonly IAdminTaskService _adminTaskService;

    public AdminScoreController(IAdminTaskService adminTaskService)
    {
        _adminTaskService = adminTaskService;
    }

    /// <summary>
    /// Puanlanmayı bekleyen (ScoreCategory == null) taskları listeler.
    /// </summary>
    [HttpGet("pending-tasks")]
    public async Task<IActionResult> GetPendingTasks()
    {
        var tasks = await _adminTaskService.GetPendingScoreTasksAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// Belirli bir taska puan kategorisi atar. (0, 1, 2, 3)
    /// </summary>
    [HttpPost("tasks/{taskId}/assign-score")]
    public async Task<IActionResult> AssignScore(Guid taskId, [FromBody] int category)
    {
        try
        {
            await _adminTaskService.AssignTaskScoreAsync(taskId, category);
            return Ok(new { message = "Puan başarıyla atandı." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kullanıcıya manuel puan ekler veya çıkarır.
    /// </summary>
    [HttpPost("users/{userId}/adjust-score")]
    public async Task<IActionResult> AdjustUserScore(Guid userId, [FromBody] AdjustScoreDto dto)
    {
        try
        {
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _adminTaskService.AdjustUserScoreAsync(userId, dto.Amount, dto.Reason, adminId);
            return Ok(new { message = "Puan işlemi başarıyla gerçekleşti." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
