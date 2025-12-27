using ailab_super_app.DTOs.Task;

namespace ailab_super_app.Services.Interfaces;

public interface IAdminTaskService
{
    Task<List<TaskListDto>> GetPendingScoreTasksAsync();
    Task AssignTaskScoreAsync(Guid taskId, int category);
}
