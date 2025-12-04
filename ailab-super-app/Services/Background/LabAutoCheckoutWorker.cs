using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ailab_super_app.Data;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums; // EntryType için eklendi
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
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Her 15 dakikada bir kontrol et
        private readonly TimeSpan _autoCheckoutThreshold = TimeSpan.FromHours(4); // 4 saat sonra otomatik çıkış yap

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
                _logger.LogInformation("Lab Auto Checkout Worker çalışıyor: {time}", DateTimeOffset.Now);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    try
                    {
                        await PerformAutoCheckoutAsync(dbContext);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Otomatik çıkış işlemi sırasında hata oluştu: {Message}", ex.Message);
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Lab Auto Checkout Worker durdu.");
        }

        private async Task PerformAutoCheckoutAsync(AppDbContext dbContext)
        {
            var now = DateTime.UtcNow;
            
            // Şu anda labda görünen tüm kullanıcıları bul
            var usersInLab = await dbContext.LabEntries
                                            .Where(le => le.ExitTime == null) // Henüz çıkış yapmamış
                                            .ToListAsync();

            foreach (var labEntry in usersInLab)
            {
                var entryTime = labEntry.EntryTime;
                var timeInLab = now - entryTime;

                if (timeInLab > _autoCheckoutThreshold)
                {
                    // Kullanıcı 4 saati aşkın süredir içeride ve çıkış yapmamış
                    _logger.LogWarning("Kullanıcı {UserId}, {EntryTime} tarihinde giriş yaptı ve {Threshold} süreyi aştı. Otomatik çıkış yapılıyor.", 
                                       labEntry.UserId, entryTime, _autoCheckoutThreshold);

                    // LabEntry kaydını güncelle
                    labEntry.ExitTime = entryTime.Add(_autoCheckoutThreshold); // Giriş zamanı + 4 saat olarak işaretle
                    labEntry.DurationMinutes = (int)_autoCheckoutThreshold.TotalMinutes;
                    labEntry.Notes = "Otomatik çıkış (Süre aşımı)";
                    
                    // RoomAccess kaydı ekle (log amaçlı)
                    // LabEntry.RfidCardId nullable olabilir, RoomAccess için zorunlu ise kontrol etmeliyiz.
                    // Şimdilik RfidCardId varsa kullan, yoksa varsayılan bir GUID veya hata (ama hata fırlatmayalım).
                    // DB migration ile RfidCardId eklendiği için null gelebilir.
                    
                    var rfidCardId = labEntry.RfidCardId ?? Guid.Empty; // DİKKAT: Bu geçici bir çözüm. Migration ile nullable yapıldıysa sorun yok.
                    
                    if (rfidCardId == Guid.Empty)
                    {
                         // Kullanıcının herhangi bir aktif kartını bulmaya çalış
                         var userCard = await dbContext.RfidCards.FirstOrDefaultAsync(c => c.UserId == labEntry.UserId);
                         rfidCardId = userCard?.Id ?? Guid.Empty;
                    }

                    if (rfidCardId != Guid.Empty && labEntry.RoomId.HasValue)
                    {
                        var newRoomAccess = new RoomAccess
                        {
                            Id = Guid.NewGuid(),
                            UserId = labEntry.UserId,
                            RoomId = labEntry.RoomId.Value,
                            RfidCardId = rfidCardId,
                            AccessedAt = labEntry.ExitTime.Value,
                            Direction = EntryType.Exit,
                            IsAuthorized = true,
                            DenyReason = "Otomatik çıkış: Süre aşımı"
                        };
                        dbContext.RoomAccesses.Add(newRoomAccess);
                    }

                    // LabCurrentOccupancy tablosundan kaldır
                    var currentOccupancy = await dbContext.LabCurrentOccupancy.FindAsync(labEntry.UserId);
                    if (currentOccupancy != null)
                    {
                        dbContext.LabCurrentOccupancy.Remove(currentOccupancy);
                    }
                }
            }
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Otomatik çıkış kontrolü tamamlandı.");
        }
    }
}