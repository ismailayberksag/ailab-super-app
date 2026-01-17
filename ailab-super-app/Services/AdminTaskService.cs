using ailab_super_app.Data;
using ailab_super_app.DTOs.Task;
using ailab_super_app.Helpers;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.Services;

public class AdminTaskService : IAdminTaskService
{
    private readonly AppDbContext _context;
    private readonly IScoringService _scoringService;

    public AdminTaskService(AppDbContext context, IScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    public async Task<List<TaskListDto>> GetPendingScoreTasksAsync()
    {
        return await _context.Tasks
            .Where(t => t.ScoreCategory == null && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.Project)
            .Include(t => t.User)
            .Select(t => new TaskListDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                AssigneeId = t.AssigneeId,
                AssigneeName = t.User != null ? t.User.FullName : "Unknown",
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "N/A"
            })
            .ToListAsync();
    }

    public async Task AssignTaskScoreAsync(Guid taskId, int category)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);
        
        if (task == null) throw new Exception("Task bulunamadı.");
        if (task.ScoreCategory.HasValue) throw new Exception("Bu task zaten puanlanmış.");
        if (category < 0 || category > 3) throw new Exception("Geçersiz puan kategorisi.");

        task.ScoreCategory = category;
        task.ScoreAssignedAt = DateTimeHelper.GetTurkeyTime();

        // Eğer task zaten "Done" durumundaysa puanı hemen işle
        if (task.Status == ailab_super_app.Models.Enums.TaskStatus.Done && !task.ScoreProcessed)
        {
            var points = _scoringService.GetPointsByCategory(category);
            await _scoringService.AddScoreAsync(task.AssigneeId, points, $"Task Score Assigned (Retroactive): {task.Title}", "Task", task.Id);
            task.ScoreProcessed = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task AdjustUserScoreAsync(Guid userId, decimal amount, string reason, Guid adminId)
    {
        // Kullanıcı kontrolü
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsDeleted) throw new Exception("Kullanıcı bulunamadı.");

        // Scoring servisi kullan
        // reason parametresine admin bilgisini ekleyebiliriz
        string formattedReason = $"{reason} (Admin Adjustment by {adminId})";
        
        // ReferenceId olarak Guid.Empty veya adminId verilebilir, şu anlık null/empty bırakıyorum çünkü task veya proje değil.
        await _scoringService.AddScoreAsync(userId, amount, formattedReason, "AdminAdjustment", null);
    }
}
