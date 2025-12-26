using ailab_super_app.Common.Exceptions;
using ailab_super_app.DTOs.BugReport;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BugReportsController : ControllerBase
{
    private readonly IBugReportService _bugReportService;
    private readonly ILogger<BugReportsController> _logger;

    public BugReportsController(IBugReportService bugReportService, ILogger<BugReportsController> logger)
    {
        _bugReportService = bugReportService;
        _logger = logger;
    }

    /// <summary>
    /// Yeni bir hata (bug) bildirimi oluşturur.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBugReportDto dto)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı." });
            }

            var reportId = await _bugReportService.CreateReportAsync(userId, dto);
            
            return Ok(new 
            { 
                id = reportId, 
                message = "Hata bildiriminiz başarıyla alındı. Teşekkür ederiz." 
            });
        }
        catch (BadRequestException ex)
        {
            // 400 Bad Request
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // 500 Internal Server Error
            _logger.LogError(ex, "Unexpected error occurred while creating BugReport.");
            return StatusCode(500, new { message = "Sistem kaynaklı bir hata oluştu. Lütfen daha sonra tekrar deneyiniz." });
        }
    }
}
