using ailab_super_app.Data;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services.Background;

public class MonthlyScoreResetWorker : BackgroundService
{
    private readonly ILogger<MonthlyScoreResetWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MonthlyScoreResetWorker(ILogger<MonthlyScoreResetWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monthly Score Reset Worker başladı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            
            // Eğer ayın 1'i ve gece yarısı ise (örn: 00:00 - 01:00 arası çalışsın)
            if (now.Day == 1 && now.Hour == 0)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await PerformResetAsync(db, now);
                }
                
                // Bir sonraki güne kadar bekle
                await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // 30 dk bir kontrol et
        }
    }

    private async Task PerformResetAsync(AppDbContext db, DateTime now)
    {
        // Önceki ayın bilgisini hazırla (Örn: 2025-11)
        var lastMonth = now.AddDays(-1);
        string period = lastMonth.ToString("yyyy-MM");

        // Bugün zaten reset yapıldı mı kontrol et
        var alreadyDone = await db.MonthlyScoreSnapshots.AnyAsync(s => s.Period == period);
        if (alreadyDone) return;

        _logger.LogInformation("{Period} dönemi için puan resetleme başlıyor...", period);

        using (var transaction = await db.Database.BeginTransactionAsync())
        {
            try
            {
                var users = await db.Users.Where(u => !u.IsDeleted).ToListAsync();

                foreach (var user in users)
                {
                    // 1. Snapshot al
                    db.MonthlyScoreSnapshots.Add(new MonthlyScoreSnapshot
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        UserName = user.FullName ?? user.UserName ?? "Unknown",
                        TotalScore = user.TotalScore,
                        Period = period,
                        SnapshotDate = now
                    });

                    // 2. Sıfırla
                    user.TotalScore = 0;
                    user.UpdatedAt = now;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("{Period} dönemi puanları başarıyla sıfırlandı.", period);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Monthly reset sırasında hata oluştu!");
            }
        }
    }
}
