using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ailab_super_app.Data;
using ailab_super_app.Helpers; // GetTurkeyTime için eklendi
using ailab_super_app.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ailab_super_app.Services.Background
{
    public class LabAutoCheckoutWorker : BackgroundService
    {
        private readonly ILogger<LabAutoCheckoutWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); 
        private readonly TimeSpan _autoCheckoutThreshold = TimeSpan.FromHours(4);

        public LabAutoCheckoutWorker(ILogger<LabAutoCheckoutWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Lab Auto Checkout Worker başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    try
                    {
                        await PerformAutoCheckoutAsync(dbContext);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Otomatik çıkış hatası");
                    }
                }
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task PerformAutoCheckoutAsync(AppDbContext dbContext)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            
            // Açık oturumları bul (ExitTime == null)
            var staleSessions = await dbContext.LabEntries
                                            .Where(le => le.ExitTime == null)
                                            .ToListAsync();

            foreach (var session in staleSessions)
            {
                var timeInLab = now - session.EntryTime;

                if (timeInLab > _autoCheckoutThreshold)
                {
                    _logger.LogWarning("Kullanıcı {UserId} otomatik çıkış yapılıyor.", session.UserId);

                    // Sadece LabEntry güncelle
                    session.ExitTime = session.EntryTime.Add(_autoCheckoutThreshold);
                    session.DurationMinutes = (int)_autoCheckoutThreshold.TotalMinutes;
                    session.Notes = "Otomatik çıkış (Süre aşımı)";
                    
                    // LabCurrentOccupancy temizle
                    var occupancy = await dbContext.LabCurrentOccupancy.FindAsync(session.UserId);
                    if (occupancy != null)
                    {
                        dbContext.LabCurrentOccupancy.Remove(occupancy);
                    }
                }
            }
            await dbContext.SaveChangesAsync();
        }
    }
}