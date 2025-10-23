using ailab_super_app.Common.Exceptions;
using ailab_super_app.Data;
using ailab_super_app.DTOs.Task;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;

namespace ailab_super_app.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<TaskService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PagedResult<TaskListDto>> GetProjectTasksAsync(Guid projectId, PaginationParams paginationParams, Guid? requestingUserId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Check access: Admin or Project Member
        if (requestingUserId.HasValue)
        {
            var isAdmin = await IsAdminAsync(requestingUserId.Value);
            var isMember = await IsMemberAsync(projectId, requestingUserId.Value);

            if (!isAdmin && !isMember)
            {
                throw new UnauthorizedAccessException("Bu projeye erişim yetkiniz yok");
            }
        }

        var query = _context.Tasks
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var tasks = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        var taskListDtos = new List<TaskListDto>();

        foreach (var task in tasks)
        {
            var assignee = await _userManager.FindByIdAsync(task.AssigneeId.ToString());

            taskListDtos.Add(new TaskListDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                AssigneeName = assignee?.FullName ?? assignee?.UserName,
                DueDate = task.DueDate,
                ProjectName = project.Name
            });
        }

        return new PagedResult<TaskListDto>
        {
            Items = taskListDtos,
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

        if (task == null)
        {
            throw new NotFoundException("Task bulunamadı");
        }

        // Check access: Admin or Project Member
        if (requestingUserId.HasValue && task.ProjectId.HasValue)
        {
            var isAdmin = await IsAdminAsync(requestingUserId.Value);
            var isMember = await IsMemberAsync(task.ProjectId.Value, requestingUserId.Value);

            if (!isAdmin && !isMember)
            {
                throw new UnauthorizedAccessException("Bu task'a erişim yetkiniz yok");
            }
        }

        return await MapToTaskDto(task);
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid createdBy)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Check access: Admin or Captain
        var isAdmin = await IsAdminAsync(createdBy);
        var isCaptain = await IsCaptainAsync(dto.ProjectId, createdBy);

        if (!isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");
        }

        // Validate assignee is project member
        var isProjectMember = await IsMemberAsync(dto.ProjectId, dto.AssigneeId);
        if (!isProjectMember)
        {
            throw new BadRequestException("Task sadece proje üyelerine atanabilir");
        }

        // Check if assignee is active
        var assignee = await _userManager.FindByIdAsync(dto.AssigneeId.ToString());
        if (assignee == null || assignee.IsDeleted || assignee.Status != UserStatus.Active)
        {
            throw new BadRequestException("Aktif olmayan kullanıcıya task atanamaz");
        }

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            ProjectId = dto.ProjectId,
            AssigneeId = dto.AssigneeId,
            CreatedBy = createdBy,
            DueDate = dto.DueDate,
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return await MapToTaskDto(task);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid requestingUserId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        if (task == null)
        {
            throw new NotFoundException("Task bulunamadı");
        }

        if (!task.ProjectId.HasValue)
        {
            throw new BadRequestException("Task bir projeye ait değil");
        }

        // Check access: Admin or Captain
        var isAdmin = await IsAdminAsync(requestingUserId);
        var isCaptain = await IsCaptainAsync(task.ProjectId.Value, requestingUserId);

        if (!isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");
        }

        // Validate at least one field is provided
        if (dto.Title == null && dto.Description == null && dto.Status == null
            && dto.AssigneeId == null && dto.DueDate == null)
        {
            throw new BadRequestException("En az bir alan güncellenmelidir");
        }

        var oldStatus = task.Status;

        // Update fields
        if (dto.Title != null)
        {
            task.Title = dto.Title;
        }

        if (dto.Description != null)
        {
            task.Description = dto.Description;
        }

        if (dto.Status.HasValue)
        {
            task.Status = dto.Status.Value;

            // Set CompletedAt when status changes to Done
            if (dto.Status.Value == TaskStatus.Done && oldStatus != TaskStatus.Done)
            {
                task.CompletedAt = DateTime.UtcNow;
                
                // Add score to assignee
                await AddScoreForTaskCompletion(task, requestingUserId);
            }
        }

        if (dto.AssigneeId.HasValue)
        {
            // Validate new assignee is project member
            var isProjectMember = await IsMemberAsync(task.ProjectId.Value, dto.AssigneeId.Value);
            if (!isProjectMember)
            {
                throw new BadRequestException("Task sadece proje üyelerine atanabilir");
            }

            // Check if new assignee is active
            var newAssignee = await _userManager.FindByIdAsync(dto.AssigneeId.Value.ToString());
            if (newAssignee == null || newAssignee.IsDeleted || newAssignee.Status != UserStatus.Active)
            {
                throw new BadRequestException("Aktif olmayan kullanıcıya task atanamaz");
            }

            task.AssigneeId = dto.AssigneeId.Value;
        }

        if (dto.DueDate.HasValue)
        {
            task.DueDate = dto.DueDate;
        }

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToTaskDto(task);
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid deletedBy)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        if (task == null)
        {
            throw new NotFoundException("Task bulunamadı");
        }

        if (!task.ProjectId.HasValue)
        {
            throw new BadRequestException("Task bir projeye ait değil");
        }

        // Check access: Admin or Captain
        var isAdmin = await IsAdminAsync(deletedBy);
        var isCaptain = await IsCaptainAsync(task.ProjectId.Value, deletedBy);

        if (!isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");
        }

        // Soft delete
        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        task.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<TaskDto> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusDto dto, Guid requestingUserId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        if (task == null)
        {
            throw new NotFoundException("Task bulunamadı");
        }

        // Check if user is assignee or admin/captain
        var isAssignee = task.AssigneeId == requestingUserId;
        var isAdmin = false;
        var isCaptain = false;

        if (task.ProjectId.HasValue)
        {
            isAdmin = await IsAdminAsync(requestingUserId);
            isCaptain = await IsCaptainAsync(task.ProjectId.Value, requestingUserId);
        }

        if (!isAssignee && !isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Sadece task'ın atandığı kişi, Captain veya Admin status değiştirebilir");
        }

        var oldStatus = task.Status;
        task.Status = dto.Status;
        task.UpdatedAt = DateTime.UtcNow;

        // Set CompletedAt when status changes to Done
        if (dto.Status == TaskStatus.Done && oldStatus != TaskStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
            
            // Add score to assignee
            await AddScoreForTaskCompletion(task, requestingUserId);
        }

        await _context.SaveChangesAsync();

        return await MapToTaskDto(task);
    }

    public async Task<List<TaskListDto>> GetMyTasksAsync(Guid userId, TaskStatus? statusFilter = null)
    {
        var query = _context.Tasks
            .Where(t => t.AssigneeId == userId && !t.IsDeleted)
            .AsQueryable();

        // Apply status filter if provided
        if (statusFilter.HasValue)
        {
            query = query.Where(t => t.Status == statusFilter.Value);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var taskListDtos = new List<TaskListDto>();

        foreach (var task in tasks)
        {
            var assignee = await _userManager.FindByIdAsync(task.AssigneeId.ToString());
            
            string? projectName = null;
            if (task.ProjectId.HasValue)
            {
                var project = await _context.Projects.FindAsync(task.ProjectId.Value);
                projectName = project?.Name;
            }

            taskListDtos.Add(new TaskListDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                AssigneeName = assignee?.FullName ?? assignee?.UserName,
                DueDate = task.DueDate,
                ProjectName = projectName
            });
        }

        return taskListDtos;
    }

    #region Private Helper Methods

    private async Task<bool> IsAdminAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, "Admin");
    }

    private async Task<bool> IsCaptainAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId
                         && pm.UserId == userId
                         && pm.Role == "Captain"
                         && !pm.IsDeleted);
    }

    private async Task<bool> IsMemberAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId
                         && pm.UserId == userId
                         && !pm.IsDeleted);
    }

    private async Task AddScoreForTaskCompletion(TaskItem task, Guid scoredBy)
    {
        // Add +10 points to assignee
        var assignee = await _userManager.FindByIdAsync(task.AssigneeId.ToString());
        if (assignee != null)
        {
            assignee.TotalScore += 10;

            // Create ScoreHistory record
            var scoreHistory = new ScoreHistory
            {
                UserId = task.AssigneeId,
                PointsChanged = 10,
                Reason = $"Task tamamlandı: {task.Title}",
                ReferenceType = "Task",
                ReferenceId = task.Id,
                CreatedBy = scoredBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.ScoreHistory.Add(scoreHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {task.AssigneeId} earned 10 points for completing task {task.Id}");
        }
    }

    private async Task<TaskDto> MapToTaskDto(TaskItem task)
    {
        var assignee = await _userManager.FindByIdAsync(task.AssigneeId.ToString());
        var creator = await _userManager.FindByIdAsync(task.CreatedBy.ToString());

        string? projectName = null;
        if (task.ProjectId.HasValue)
        {
            if (task.Project != null)
            {
                projectName = task.Project.Name;
            }
            else
            {
                var project = await _context.Projects.FindAsync(task.ProjectId.Value);
                projectName = project?.Name;
            }
        }

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            AssigneeId = task.AssigneeId,
            AssigneeName = assignee?.FullName ?? assignee?.UserName,
            CreatedBy = task.CreatedBy,
            CreatedByName = creator?.FullName ?? creator?.UserName,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt
        };
    }

    #endregion
}

