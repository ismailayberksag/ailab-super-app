using ailab_super_app.Data;
using ailab_super_app.Helpers;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services.Background;

public class DeadlinePenaltyWorker : BackgroundService
{
    private readonly ILogger<DeadlinePenaltyWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Günlük çalışır

    public DeadlinePenaltyWorker(ILogger<DeadlinePenaltyWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deadline Penalty Worker başladı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Her gün 03:00'te çalışması için hesaplama (Basitçe 24 saatte bir çalışır)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var scoringService = scope.ServiceProvider.GetRequiredService<IScoringService>();
                    
                    await ApplyDailyPenaltiesAsync(dbContext, scoringService);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deadline cezası uygulanırken hata oluştu.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ApplyDailyPenaltiesAsync(AppDbContext db, IScoringService scoringService)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var todayDate = now.Date;

        // --- 1. TASK PENALTIES ---
        var overdueTasks = await db.Tasks
            .Where(t => t.Status != ailab_super_app.Models.Enums.TaskStatus.Done 
                     && t.Status != ailab_super_app.Models.Enums.TaskStatus.Cancelled 
                     && t.DueDate != null 
                     && t.DueDate < now 
                     && !t.IsDeleted)
            .ToListAsync();

        foreach (var task in overdueTasks)
        {
            var alreadyPenalized = await db.ScoreHistory
                .AnyAsync(sh => sh.ReferenceId == task.Id 
                             && sh.Reason.StartsWith("Deadline Penalty") 
                             && sh.CreatedAt.Date == todayDate);

            if (!alreadyPenalized)
            {
                await scoringService.AddScoreAsync(task.AssigneeId, -0.1m, $"Deadline Penalty: {task.Title}", "Task", task.Id);
            }
        }

        // --- 2. REPORT PENALTIES ---
        // Tarihi geçmiş ve henüz ceza uygulanmamış proje-talep eşleşmeleri
        var overdueReportRequests = await db.ReportRequestProjects
            .Include(rrp => rrp.ReportRequest)
            .Include(rrp => rrp.Project)
                .ThenInclude(p => p.Members)
            .Where(rrp => !rrp.PenaltyApplied 
                       && rrp.ReportRequest.DueDate != null 
                       && rrp.ReportRequest.DueDate < now
                       && !rrp.ReportRequest.IsDeleted)
            .ToListAsync();

        foreach (var reqProject in overdueReportRequests)
        {
            // Bu proje bu talep için geçerli (Rejected olmayan ve Active olan) bir rapor yüklemiş mi?
            // Not: Report tablosunda RequestId ve ProjectId var.
            bool hasValidSubmission = await db.Reports
                .AnyAsync(r => r.RequestId == reqProject.ReportRequestId 
                            && r.ProjectId == reqProject.ProjectId 
                            && r.IsActive 
                            && r.Status != ReportStatus.Rejected);

            if (!hasValidSubmission)
            {
                // Ceza Uygula: Projedeki herkese -5
                var members = reqProject.Project.Members.Where(m => !m.IsDeleted).ToList();
                foreach (var member in members)
                {
                    await scoringService.AddScoreAsync(
                        member.UserId, 
                        -5.0m, 
                        $"Rapor Teslim Cezası: {reqProject.ReportRequest.Title}", 
                        "ReportDeadline", 
                        reqProject.ReportRequestId
                    );
                }

                // Cezayı işaretle ki tekrar kesilmesin
                reqProject.PenaltyApplied = true;
                reqProject.PenaltyAppliedAt = now;
            }
        }

        // Değişiklikleri kaydet (PenaltyApplied update için)
        await db.SaveChangesAsync();
    }
}
