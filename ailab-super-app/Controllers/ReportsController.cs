using ailab_super_app.DTOs.Report;
using ailab_super_app.Helpers;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Kullanıcı ID'si bulunamadı.");
            }
            return userId;
        }

        // 1. Rapor Talebi Oluşturma (Sadece Admin)
        [HttpPost("requests")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateReportRequestDto dto)
        {
            var result = await _reportService.CreateRequestAsync(GetCurrentUserId(), dto);
            return Ok(result);
        }

        // 2. Bana Atanan Rapor Talepleri (Projelerimin talepleri)
        [HttpGet("requests/me")]
        public async Task<IActionResult> GetMyAssignedRequests([FromQuery] PaginationParams pagination)
        {
            var result = await _reportService.GetMyAssignedRequestsAsync(GetCurrentUserId(), pagination);
            return Ok(result);
        }

        // 3. Bir Talebin Detayını Getir
        [HttpGet("requests/{id}")]
        public async Task<IActionResult> GetRequestById(Guid id)
        {
            var result = await _reportService.GetRequestByIdAsync(id, GetCurrentUserId());
            return Ok(result);
        }

        // 4. Rapor Yükleme (Sadece Captain - Servis katmanında kontrol ediliyor)
        [HttpPost("upload")]
        public async Task<IActionResult> UploadReport([FromForm] UploadReportDto dto)
        {
            // PDF validasyonu
            if (dto.PdfFile == null || dto.PdfFile.Length == 0)
                return BadRequest("Lütfen geçerli bir PDF dosyası yükleyin.");

            if (dto.PdfFile.ContentType != "application/pdf")
                return BadRequest("Sadece PDF dosyaları kabul edilmektedir.");

            // Maksimum boyut kontrolü (örn. 10MB)
            if (dto.PdfFile.Length > 10 * 1024 * 1024)
                return BadRequest("Dosya boyutu 10MB'ı geçemez.");

            var result = await _reportService.UploadReportAsync(GetCurrentUserId(), dto);
            return Ok(result);
        }

        // 5. Rapor İndirme Linki Alma
        [HttpGet("{id}/download-url")]
        public async Task<IActionResult> GetDownloadUrl(Guid id)
        {
            var url = await _reportService.GetSignedDownloadUrlAsync(id, GetCurrentUserId());
            return Ok(new { Url = url });
        }

        // 6. Rapor İnceleme / Onaylama (Sadece Admin)
        [HttpPut("{id}/review")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ReviewReport(Guid id, [FromBody] ReviewReportDto dto)
        {
            var result = await _reportService.ReviewReportAsync(id, GetCurrentUserId(), dto);
            return Ok(result);
        }

        // 7. Rapor Detayı Getirme
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(Guid id)
        {
            var result = await _reportService.GetReportByIdAsync(id, GetCurrentUserId());
            return Ok(result);
        }

        // 8. Admin için Tüm Talepler (İsteğe bağlı, CreatedBy ile filtreleyebiliriz)
        [HttpGet("requests/admin")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetAdminCreatedRequests([FromQuery] PaginationParams pagination)
        {
            var result = await _reportService.GetMyCreatedRequestsAsync(GetCurrentUserId(), pagination);
            return Ok(result);
        }
    }
}