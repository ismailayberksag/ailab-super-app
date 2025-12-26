using ailab_super_app.Common.Exceptions;
using ailab_super_app.Data;
using ailab_super_app.DTOs.BugReport;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services;

public class BugReportService : IBugReportService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BugReportService> _logger;

    public BugReportService(AppDbContext context, IMemoryCache cache, ILogger<BugReportService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> CreateReportAsync(Guid userId, CreateBugReportDto dto)
    {
        string cacheKey = $"BugReport_Throttle_{userId}";

        // 🛡️ LAYER 1: Application-Level Throttling (MemoryCache)
        // Kullanıcı her 30 saniyede en fazla 1 istek atabilir.
        if (_cache.TryGetValue(cacheKey, out _))
        {
            throw new BadRequestException("Çok sık hata bildirimi gönderiyorsunuz. Lütfen biraz bekleyin.");
        }

        // 🛡️ LAYER 2: Database-Level Quota (Business Logic)
        // Son 1 saatte en fazla 5 bildirim sınırı.
        var oneHourAgo = DateTimeHelper.GetTurkeyTime().AddHours(-1);
        var recentReportsCount = await _context.BugReports
            .CountAsync(x => x.ReportedByUserId == userId && x.ReportedAt > oneHourAgo);

        if (recentReportsCount >= 5)
        {
            throw new BadRequestException("Saatlik hata bildirimi limitinize ulaştınız (Maksimum 5).");
        }

        var report = new BugReport
        {
            Id = Guid.NewGuid(),
            Platform = dto.Platform,
            PageInfo = dto.PageInfo,
            BugType = dto.BugType,
            Description = dto.Description,
            ReportedByUserId = userId,
            ReportedAt = DateTimeHelper.GetTurkeyTime(),
            IsResolved = false
        };

        try
        {
            _context.BugReports.Add(report);
            await _context.SaveChangesAsync();

            // Başarılı kayıttan sonra throttle'ı başlat
            _cache.Set(cacheKey, true, TimeSpan.FromSeconds(30));

            return report.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BugReport veritabanına kaydedilirken hata oluştu. UserId: {UserId}", userId);
            throw; // Üst katmanda yakalanıp 500 olarak dönecek
        }
    }
}
