using ailab_super_app.DTOs.Role;

namespace ailab_super_app.Services.Interfaces;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllRolesAsync();
    Task<RoleDto> GetRoleByIdAsync(Guid roleId);
    Task<RoleDto> GetRoleByNameAsync(string roleName);
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleDto dto);
    Task DeleteRoleAsync(Guid roleId);
    Task AssignRoleToUserAsync(AssignRoleDto dto);
    Task RemoveRoleFromUserAsync(Guid userId, string roleName);
    Task<List<string>> GetUserRolesAsync(Guid userId);
}

