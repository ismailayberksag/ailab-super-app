using ailab_super_app.DTOs.Project;
using ailab_super_app.Helpers;

namespace ailab_super_app.Services.Interfaces;

public interface IProjectService
{
    // Project CRUD
    Task<PagedResult<ProjectListDto>> GetProjectsAsync(PaginationParams paginationParams);
    Task<ProjectDto> GetProjectByIdAsync(Guid projectId, Guid? requestingUserId);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, Guid createdBy);
    Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectDto dto, Guid requestingUserId);
    Task DeleteProjectAsync(Guid projectId, Guid deletedBy);

    // Member Management
    Task<List<ProjectMemberDto>> GetProjectMembersAsync(Guid projectId, Guid? requestingUserId);
    Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto dto, Guid addedBy);
    Task RemoveMemberAsync(Guid projectId, Guid userId, Guid removedBy);
    Task<ProjectMemberDto> UpdateMemberRoleAsync(Guid projectId, Guid userId, UpdateProjectMemberRoleDto dto, Guid updatedBy);
    Task TransferOwnershipAsync(Guid projectId, TransferOwnershipDto dto, Guid requestedBy);

    // User Projects
    Task<List<ProjectListDto>> GetUserProjectsAsync(Guid userId, string? roleFilter = null);
}

