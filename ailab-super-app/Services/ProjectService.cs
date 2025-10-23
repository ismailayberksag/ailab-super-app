using ailab_super_app.Common.Exceptions;
using ailab_super_app.Data;
using ailab_super_app.DTOs.Project;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PagedResult<ProjectListDto>> GetProjectsAsync(PaginationParams paginationParams)
    {
        var query = _context.Projects
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var projects = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        var projectListDtos = new List<ProjectListDto>();

        foreach (var project in projects)
        {
            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id && !pm.IsDeleted)
                .Include(pm => pm.User)
                .ToListAsync();

            var captainNames = members
                .Where(pm => pm.Role == "Captain")
                .Select(pm => pm.User.FullName ?? pm.User.UserName ?? "Unknown")
                .ToList();

            var taskCount = await _context.Tasks
                .CountAsync(t => t.ProjectId == project.Id && !t.IsDeleted);

            string? createdByName = null;
            if (project.CreatedBy.HasValue)
            {
                var creator = await _userManager.FindByIdAsync(project.CreatedBy.Value.ToString());
                createdByName = creator?.FullName ?? creator?.UserName;
            }

            // Truncate description to 100 chars
            var description = project.Description?.Length > 100
                ? project.Description.Substring(0, 100) + "..."
                : project.Description;

            projectListDtos.Add(new ProjectListDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = description,
                CreatedAt = project.CreatedAt,
                CreatedBy = project.CreatedBy,
                CreatedByName = createdByName,
                MemberCount = members.Count,
                TaskCount = taskCount,
                CaptainNames = captainNames
            });
        }

        return new PagedResult<ProjectListDto>
        {
            Items = projectListDtos,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize
        };
    }

    public async Task<ProjectDto> GetProjectByIdAsync(Guid projectId, Guid? requestingUserId)
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

        return await MapToProjectDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, Guid createdBy)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return await MapToProjectDto(project);
    }

    public async Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectDto dto, Guid requestingUserId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Check access: Admin or Captain
        var isAdmin = await IsAdminAsync(requestingUserId);
        var isCaptain = await IsCaptainAsync(projectId, requestingUserId);

        if (!isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");
        }

        // Validate at least one field is provided
        if (dto.Name == null && dto.Description == null)
        {
            throw new BadRequestException("En az bir alan güncellenmelidir");
        }

        // Update fields
        if (dto.Name != null)
        {
            project.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            project.Description = dto.Description;
        }

        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToProjectDto(project);
    }

    public async Task DeleteProjectAsync(Guid projectId, Guid deletedBy)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Check for active tasks
        var hasActiveTasks = await _context.Tasks
            .AnyAsync(t => t.ProjectId == projectId
                        && t.Status != TaskStatus.Done
                        && t.Status != TaskStatus.Cancelled
                        && !t.IsDeleted);

        if (hasActiveTasks)
        {
            throw new BadRequestException("Proje aktif tasklar içerdiği için silinemez. Önce taskları tamamlayın veya iptal edin");
        }

        // Soft delete
        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;
        project.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<List<ProjectMemberDto>> GetProjectMembersAsync(Guid projectId, Guid? requestingUserId)
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

        var members = await _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId && !pm.IsDeleted)
            .Include(pm => pm.User)
            .ToListAsync();

        return members.Select(pm => new ProjectMemberDto
        {
            Id = pm.Id,
            UserId = pm.UserId,
            UserName = pm.User.UserName,
            FullName = pm.User.FullName,
            Email = pm.User.Email,
            Role = pm.Role,
            AddedAt = pm.AddedAt
        }).ToList();
    }

    public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto dto, Guid addedBy)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Check access: Admin or Captain
        var isAdmin = await IsAdminAsync(addedBy);
        var isCaptain = await IsCaptainAsync(projectId, addedBy);

        if (!isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");
        }

        // Validate role
        if (dto.Role != "Captain" && dto.Role != "Member")
        {
            throw new BadRequestException("Role 'Captain' veya 'Member' olmalıdır");
        }

        // Check if user exists and is active
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user == null || user.IsDeleted)
        {
            throw new NotFoundException("Kullanıcı bulunamadı");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new BadRequestException("Aktif olmayan kullanıcı projeye eklenemez");
        }

        // Check if user is already a member
        var existingMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == dto.UserId && !pm.IsDeleted);

        if (existingMember != null)
        {
            throw new ConflictException("Kullanıcı zaten bu projede mevcut");
        }

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = dto.UserId,
            Role = dto.Role,
            AddedAt = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        return new ProjectMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Role = member.Role,
            AddedAt = member.AddedAt
        };
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid userId, Guid removedBy)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Check access: Admin or Captain
        var isAdmin = await IsAdminAsync(removedBy);
        var isCaptain = await IsCaptainAsync(projectId, removedBy);

        if (!isAdmin && !isCaptain)
        {
            throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");
        }

        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && !pm.IsDeleted);

        if (member == null)
        {
            throw new NotFoundException("Üye bulunamadı");
        }

        // Check if member has assigned tasks
        var hasAssignedTasks = await _context.Tasks
            .AnyAsync(t => t.ProjectId == projectId && t.AssigneeId == userId && !t.IsDeleted);

        if (hasAssignedTasks)
        {
            throw new BadRequestException("Üyenin atanmış taskları var. Önce taskları yeniden atayın veya iptal edin");
        }

        // Check if removing last captain
        if (member.Role == "Captain")
        {
            var captainCount = await _context.ProjectMembers
                .CountAsync(pm => pm.ProjectId == projectId && pm.Role == "Captain" && !pm.IsDeleted);

            if (captainCount <= 1)
            {
                throw new BadRequestException("Son Captain kaldırılamaz");
            }
        }

        // Soft delete
        member.IsDeleted = true;
        member.DeletedAt = DateTime.UtcNow;
        member.DeletedBy = removedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<ProjectMemberDto> UpdateMemberRoleAsync(Guid projectId, Guid userId, UpdateProjectMemberRoleDto dto, Guid updatedBy)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            throw new NotFoundException("Proje bulunamadı");
        }

        // Only Admin can update roles (Captain assignment)
        var isAdmin = await IsAdminAsync(updatedBy);

        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Sadece Admin rol değiştirebilir");
        }

        // Validate role
        if (dto.Role != "Captain" && dto.Role != "Member")
        {
            throw new BadRequestException("Role 'Captain' veya 'Member' olmalıdır");
        }

        var member = await _context.ProjectMembers
            .Include(pm => pm.User)
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && !pm.IsDeleted);

        if (member == null)
        {
            throw new NotFoundException("Üye bulunamadı");
        }

        // Check if demoting last captain
        if (member.Role == "Captain" && dto.Role == "Member")
        {
            var captainCount = await _context.ProjectMembers
                .CountAsync(pm => pm.ProjectId == projectId && pm.Role == "Captain" && !pm.IsDeleted);

            if (captainCount <= 1)
            {
                throw new BadRequestException("Son Captain Member yapılamaz");
            }
        }

        member.Role = dto.Role;
        await _context.SaveChangesAsync();

        return new ProjectMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            UserName = member.User.UserName,
            FullName = member.User.FullName,
            Email = member.User.Email,
            Role = member.Role,
            AddedAt = member.AddedAt
        };
    }

    public async Task<List<ProjectListDto>> GetUserProjectsAsync(Guid userId, string? roleFilter = null)
    {
        var query = _context.ProjectMembers
            .Where(pm => pm.UserId == userId && !pm.IsDeleted)
            .Include(pm => pm.Project)
            .AsQueryable();

        // Apply role filter if provided
        if (!string.IsNullOrEmpty(roleFilter))
        {
            query = query.Where(pm => pm.Role == roleFilter);
        }

        var memberships = await query.ToListAsync();

        var projectListDtos = new List<ProjectListDto>();

        foreach (var membership in memberships)
        {
            var project = membership.Project;

            if (project.IsDeleted)
                continue;

            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id && !pm.IsDeleted)
                .Include(pm => pm.User)
                .ToListAsync();

            var captainNames = members
                .Where(pm => pm.Role == "Captain")
                .Select(pm => pm.User.FullName ?? pm.User.UserName ?? "Unknown")
                .ToList();

            var taskCount = await _context.Tasks
                .CountAsync(t => t.ProjectId == project.Id && !t.IsDeleted);

            string? createdByName = null;
            if (project.CreatedBy.HasValue)
            {
                var creator = await _userManager.FindByIdAsync(project.CreatedBy.Value.ToString());
                createdByName = creator?.FullName ?? creator?.UserName;
            }

            // Truncate description
            var description = project.Description?.Length > 100
                ? project.Description.Substring(0, 100) + "..."
                : project.Description;

            projectListDtos.Add(new ProjectListDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = description,
                CreatedAt = project.CreatedAt,
                CreatedBy = project.CreatedBy,
                CreatedByName = createdByName,
                MemberCount = members.Count,
                TaskCount = taskCount,
                CaptainNames = captainNames
            });
        }

        return projectListDtos.OrderByDescending(p => p.CreatedAt).ToList();
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

    private async Task<ProjectDto> MapToProjectDto(Project project)
    {
        var members = await _context.ProjectMembers
            .Where(pm => pm.ProjectId == project.Id && !pm.IsDeleted)
            .Include(pm => pm.User)
            .ToListAsync();

        var captains = members
            .Where(pm => pm.Role == "Captain")
            .Select(pm => new ProjectMemberDto
            {
                Id = pm.Id,
                UserId = pm.UserId,
                UserName = pm.User.UserName,
                FullName = pm.User.FullName,
                Email = pm.User.Email,
                Role = pm.Role,
                AddedAt = pm.AddedAt
            }).ToList();

        var regularMembers = members
            .Where(pm => pm.Role == "Member")
            .Select(pm => new ProjectMemberDto
            {
                Id = pm.Id,
                UserId = pm.UserId,
                UserName = pm.User.UserName,
                FullName = pm.User.FullName,
                Email = pm.User.Email,
                Role = pm.Role,
                AddedAt = pm.AddedAt
            }).ToList();

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == project.Id && !t.IsDeleted)
            .ToListAsync();

        string? createdByName = null;
        if (project.CreatedBy.HasValue)
        {
            var creator = await _userManager.FindByIdAsync(project.CreatedBy.Value.ToString());
            createdByName = creator?.FullName ?? creator?.UserName;
        }

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            CreatedBy = project.CreatedBy,
            CreatedByName = createdByName,
            TaskCount = tasks.Count,
            TodoTaskCount = tasks.Count(t => t.Status == TaskStatus.Todo),
            InProgressTaskCount = tasks.Count(t => t.Status == TaskStatus.InProgress),
            DoneTaskCount = tasks.Count(t => t.Status == TaskStatus.Done),
            CancelledTaskCount = tasks.Count(t => t.Status == TaskStatus.Cancelled),
            MemberCount = members.Count,
            Captains = captains,
            Members = regularMembers
        };
    }

    #endregion
}

