using ailab_super_app.Common.Exceptions;
using ailab_super_app.Data;
using ailab_super_app.DTOs.Task;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;

namespace ailab_super_app.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(AppDbContext context, ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<TaskListDto>> GetProjectTasksAsync(Guid projectId, PaginationParams paginationParams, Guid? requestingUserId)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project == null) throw new NotFoundException("Proje bulunamadı");

        var query = _context.Tasks
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var tasks = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Include(t => t.Project)
            .Include(t => t.User) // AssigneeName için User'ı include et
            .ToListAsync();

        var taskDtos = tasks.Select(t => new TaskListDto
        {
            Id = t.Id,
            Title = t.Title,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            AssigneeId = t.AssigneeId,
            AssigneeName = t.User?.FullName ?? t.User?.UserName,
            DueDate = t.DueDate,
            ProjectId = t.ProjectId,
            ProjectName = t.Project?.Name
        }).ToList();

        return new PagedResult<TaskListDto>
        {
            Items = taskDtos,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize
        };
    }

    public async Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid? requestingUserId)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        if (task == null) throw new NotFoundException("Görev bulunamadı");

        return MapToTaskDto(task);
    }

    public async Task<List<TaskListDto>> GetMyTasksAsync(Guid userId, TaskStatus? status = null)
    {
        var query = _context.Tasks
            .Where(t => t.AssigneeId == userId && !t.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.Project)
            .Include(t => t.User) // AssigneeName için include
            .ToListAsync();

        return tasks.Select(t => new TaskListDto
        {
            Id = t.Id,
            Title = t.Title,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            AssigneeId = t.AssigneeId,
            AssigneeName = t.User?.FullName ?? t.User?.UserName,
            DueDate = t.DueDate,
            ProjectId = t.ProjectId,
            ProjectName = t.Project?.Name
        }).ToList();
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid createdBy)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = TaskStatus.Todo,
            CreatedAt = now,
            DueDate = dto.DueDate,
            AssigneeId = dto.AssigneeId,
            ProjectId = dto.ProjectId,
            CreatedBy = createdBy
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return MapToTaskDto(task);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid requestingUserId)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);
        if (task == null) throw new NotFoundException("Görev bulunamadı");

        if (dto.Title != null) task.Title = dto.Title;
        if (dto.Description != null) task.Description = dto.Description;
        if (dto.Status.HasValue) task.Status = dto.Status.Value;
        if (dto.DueDate.HasValue) task.DueDate = dto.DueDate.Value;
        if (dto.AssigneeId.HasValue) task.AssigneeId = dto.AssigneeId.Value;

        task.UpdatedAt = now;
        if (task.Status == TaskStatus.Done && !task.CompletedAt.HasValue)
        {
            task.CompletedAt = now;
        }

        await _context.SaveChangesAsync();
        return MapToTaskDto(task);
    }

    public async Task<TaskDto> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusDto dto, Guid requestingUserId)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);
        if (task == null) throw new NotFoundException("Görev bulunamadı");

        task.Status = dto.Status;
        task.UpdatedAt = now;

        if (task.Status == TaskStatus.Done)
        {
            task.CompletedAt = now;
        }
        else
        {
            task.CompletedAt = null;
        }

        await _context.SaveChangesAsync();
        return MapToTaskDto(task);
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid requestingUserId)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);
        if (task == null) throw new NotFoundException("Görev bulunamadı");

        task.IsDeleted = true;
        task.DeletedAt = now;
        task.DeletedBy = requestingUserId;

        await _context.SaveChangesAsync();
    }

    private TaskDto MapToTaskDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            AssigneeId = task.AssigneeId,
            CreatedBy = task.CreatedBy,
            ProjectId = task.ProjectId
        };
    }
}
