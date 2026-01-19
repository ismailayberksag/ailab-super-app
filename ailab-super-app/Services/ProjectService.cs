using ailab_super_app.Common.Constants;
using ailab_super_app.Common.Exceptions;
using ailab_super_app.Data;
using ailab_super_app.DTOs.Project;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ailab_super_app.Models.Enums.TaskStatus;
using UserStatus = ailab_super_app.Models.UserStatus;

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
                .Where(pm => pm.Role == ProjectRoles.Captain)
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

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        if (requestingUserId.HasValue)
        {
            var isAdmin = await IsAdminAsync(requestingUserId.Value);
            var isMember = await IsMemberAsync(projectId, requestingUserId.Value);

            if (!isAdmin && !isMember) throw new UnauthorizedAccessException("Bu projeye erişim yetkiniz yok");
        }

        return await MapToProjectDto(project);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, Guid createdBy)
    {
        // 1. Captain user'ın varlığını kontrol et
        var captainUser = await _userManager.FindByIdAsync(dto.CaptainUserId.ToString());
        if (captainUser == null || captainUser.IsDeleted)
        {
            throw new NotFoundException($"Captain olarak atanacak kullanıcı bulunamadı: {dto.CaptainUserId}");
        }

        // 2. Transaction başlat
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTimeHelper.GetTurkeyTime();

            // 3. Proje oluştur
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedBy = createdBy,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // 4. Captain'i projeye ekle
            var captainMember = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = dto.CaptainUserId,
                Role = ProjectRoles.Captain,
                AddedAt = now
            };

            _context.ProjectMembers.Add(captainMember);
            await _context.SaveChangesAsync();

            // 5. Transaction commit
            await transaction.CommitAsync();

            return await MapToProjectDto(project);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectDto dto, Guid requestingUserId)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        var isAdmin = await IsAdminAsync(requestingUserId);
        var isCaptain = await IsCaptainAsync(projectId, requestingUserId);

        if (!isAdmin && !isCaptain) throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");

        if (dto.Name == null && dto.Description == null) throw new BadRequestException("En az bir alan güncellenmelidir");

        if (dto.Name != null) project.Name = dto.Name;
        if (dto.Description != null) project.Description = dto.Description;

        project.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return await MapToProjectDto(project);
    }

    public async Task DeleteProjectAsync(Guid projectId, Guid deletedBy)
    {
        var now = DateTimeHelper.GetTurkeyTime();
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        var hasActiveTasks = await _context.Tasks
            .AnyAsync(t => t.ProjectId == projectId
                        && t.Status != TaskStatus.Done
                        && t.Status != TaskStatus.Cancelled
                        && !t.IsDeleted);

        if (hasActiveTasks) throw new BadRequestException("Proje aktif tasklar içerdiği için silinemez.");

        project.IsDeleted = true;
        project.DeletedAt = now;
        project.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<List<ProjectMemberDto>> GetProjectMembersAsync(Guid projectId, Guid? requestingUserId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        if (requestingUserId.HasValue)
        {
            var isAdmin = await IsAdminAsync(requestingUserId.Value);
            var isMember = await IsMemberAsync(projectId, requestingUserId.Value);

            if (!isAdmin && !isMember) throw new UnauthorizedAccessException("Bu projeye erişim yetkiniz yok");
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
        var now = DateTimeHelper.GetTurkeyTime();
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        var isAdmin = await IsAdminAsync(addedBy);
        var isCaptain = await IsCaptainAsync(projectId, addedBy);

        if (!isAdmin && !isCaptain) throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");

        if (dto.Role != ProjectRoles.Captain && dto.Role != ProjectRoles.Member)
            throw new BadRequestException($"Role '{ProjectRoles.Captain}' veya '{ProjectRoles.Member}' olmalıdır");

        // Captain Uniqueness Check
        if (dto.Role == ProjectRoles.Captain)
        {
            var hasCaptain = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.Role == ProjectRoles.Captain && !pm.IsDeleted);

            if (hasCaptain)
                throw new InvalidOperationException("Bu projede zaten bir Captain var. Sadece bir Captain olabilir.");
        }

        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user == null || user.IsDeleted) throw new NotFoundException("Kullanıcı bulunamadı");

        if (user.Status != UserStatus.Active) throw new BadRequestException("Aktif olmayan kullanıcı projeye eklenemez");

        var existingMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == dto.UserId && !pm.IsDeleted);

        if (existingMember != null) throw new ConflictException("Kullanıcı zaten bu projede mevcut");

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = dto.UserId,
            Role = dto.Role,
            AddedAt = now
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
        var now = DateTimeHelper.GetTurkeyTime();
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        var isAdmin = await IsAdminAsync(removedBy);
        var isCaptain = await IsCaptainAsync(projectId, removedBy);

        if (!isAdmin && !isCaptain) throw new UnauthorizedAccessException("Bu işlem için Captain yetkisi gerekli");

        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && !pm.IsDeleted);

        if (member == null) throw new NotFoundException("Üye bulunamadı");

        // Captain Protection
        if (member.Role == ProjectRoles.Captain)
        {
            throw new InvalidOperationException("Captain rolündeki kişi projeden çıkarılamaz. Önce ownership transfer yapın.");
        }

        var hasAssignedTasks = await _context.Tasks
            .AnyAsync(t => t.ProjectId == projectId && t.AssigneeId == userId && !t.IsDeleted);

        if (hasAssignedTasks) throw new BadRequestException("Üyenin atanmış taskları var.");

        member.IsDeleted = true;
        member.DeletedAt = now;
        member.DeletedBy = removedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<ProjectMemberDto> UpdateMemberRoleAsync(Guid projectId, Guid userId, UpdateProjectMemberRoleDto dto, Guid updatedBy)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null) throw new NotFoundException("Proje bulunamadı");

        var isAdmin = await IsAdminAsync(updatedBy);
        if (!isAdmin) throw new UnauthorizedAccessException("Sadece Admin rol değiştirebilir");

        if (dto.Role != ProjectRoles.Captain && dto.Role != ProjectRoles.Member)
            throw new BadRequestException($"Role '{ProjectRoles.Captain}' veya '{ProjectRoles.Member}' olmalıdır");

        var member = await _context.ProjectMembers
            .Include(pm => pm.User)
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && !pm.IsDeleted);

        if (member == null) throw new NotFoundException("Üye bulunamadı");

        // Captain Uniqueness Check during Update
        if (member.Role == ProjectRoles.Member && dto.Role == ProjectRoles.Captain)
        {
            var hasCaptain = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.Role == ProjectRoles.Captain && !pm.IsDeleted);

            if (hasCaptain)
                throw new InvalidOperationException("Bu projede zaten bir Captain var. Başka birini Captain yapamazsınız.");
        }

        if (member.Role == ProjectRoles.Captain && dto.Role == ProjectRoles.Member)
        {
             // Projede en az 1 kaptan kalmalı kontrolü yapabiliriz ama zaten Captain transfer mekanizması var.
             // Buradan direct Member'a düşürmek yerine TransferOwnership kullanılması daha doğru olur.
             // Ancak Admin'in acil durumda yetkiyi alması gerekebilir. O yüzden izin veriyoruz
             // fakat sistemin kaptansız kalma riskini göze alıyoruz (Admin'in bildiği varsayımıyla).
             // Yine de uyarı niteliğinde:
             _logger.LogWarning($"Admin {updatedBy} removed Captain role from {userId} in project {projectId}");
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

    public async Task TransferOwnershipAsync(Guid projectId, TransferOwnershipDto dto, Guid requestedBy)
    {
        // 1. Yetki kontrolü (Sadece Admin)
        var isAdmin = await IsAdminAsync(requestedBy);
        if (!isAdmin) throw new UnauthorizedAccessException("Sadece Admin ownership transfer işlemi yapabilir.");

        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) throw new NotFoundException("Proje bulunamadı");

        // 2. Mevcut Captain kontrolü
        var currentCaptain = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == dto.CurrentCaptainId && !pm.IsDeleted);

        if (currentCaptain == null || currentCaptain.Role != ProjectRoles.Captain)
            throw new InvalidOperationException("Belirtilen kullanıcı bu projenin aktif Captain'ı değil");

        // 3. Yeni Captain kontrolü
        var newCaptain = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == dto.NewCaptainId && !pm.IsDeleted);

        if (newCaptain == null)
            throw new InvalidOperationException("Yeni Captain atanacak kişi projenin üyesi olmalıdır");

        if (newCaptain.Role == ProjectRoles.Captain)
            throw new InvalidOperationException("Bu kullanıcı zaten Captain");

        // 4. Transaction ile role swap
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            currentCaptain.Role = ProjectRoles.Member;
            newCaptain.Role = ProjectRoles.Captain;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Ownership transferred from {dto.CurrentCaptainId} to {dto.NewCaptainId} in project {projectId}");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ProjectListDto>> GetUserProjectsAsync(Guid userId, string? roleFilter = null)
    {
        var query = _context.ProjectMembers
            .Where(pm => pm.UserId == userId && !pm.IsDeleted)
            .Include(pm => pm.Project)
            .AsQueryable();

        if (!string.IsNullOrEmpty(roleFilter))
        {
            query = query.Where(pm => pm.Role == roleFilter);
        }

        var memberships = await query.ToListAsync();
        var projectListDtos = new List<ProjectListDto>();

        foreach (var membership in memberships)
        {
            var project = membership.Project;
            if (project.IsDeleted) continue;

            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id && !pm.IsDeleted)
                .Include(pm => pm.User)
                .ToListAsync();

            var captainNames = members
                .Where(pm => pm.Role == ProjectRoles.Captain)
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
                         && pm.Role == ProjectRoles.Captain
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
            .Where(pm => pm.Role == ProjectRoles.Captain)
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
            .Where(pm => pm.Role == ProjectRoles.Member)
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
