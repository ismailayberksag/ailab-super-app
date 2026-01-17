using ailab_super_app.DTOs.Task;
using ailab_super_app.Helpers;
using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;

namespace ailab_super_app.Services.Interfaces;

public interface ITaskService
{
    // Task CRUD
    Task<PagedResult<TaskListDto>> GetProjectTasksAsync(Guid projectId, PaginationParams paginationParams, Guid? requestingUserId);
    Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid? requestingUserId);
    Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid createdBy);
    Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid requestingUserId);
    Task DeleteTaskAsync(Guid taskId, Guid deletedBy);

    // Member Task Operations
    Task<TaskDto> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusDto dto, Guid requestingUserId);
    Task<List<TaskListDto>> GetMyTasksAsync(Guid userId, TaskStatus? statusFilter = null);
    
    // Admin & History
    Task<List<TaskListDto>> GetUserTaskHistoryAsync(Guid userId);
}

