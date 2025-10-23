using ailab_super_app.Data;
using ailab_super_app.DTOs.Role;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ailab_super_app.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<AppRole> roleManager,
        UserManager<User> userManager,
        AppDbContext context,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
            
            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                Permissions = DeserializePermissions(role.Permissions),
                UserCount = userCount
            });
        }

        return roleDtos;
    }

    public async Task<RoleDto> GetRoleByIdAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role == null)
        {
            throw new Exception("Rol bulunamadı");
        }

        var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            Permissions = DeserializePermissions(role.Permissions),
            UserCount = userCount
        };
    }

    public async Task<RoleDto> GetRoleByNameAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);

        if (role == null)
        {
            throw new Exception("Rol bulunamadı");
        }

        var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            Permissions = DeserializePermissions(role.Permissions),
            UserCount = userCount
        };
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        // Check if role already exists
        var existingRole = await _roleManager.FindByNameAsync(dto.Name);
        if (existingRole != null)
        {
            throw new Exception("Bu rol adı zaten kullanılıyor");
        }

        var role = new AppRole
        {
            Name = dto.Name,
            Description = dto.Description,
            Permissions = SerializePermissions(dto.Permissions)
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Rol oluşturulamadı: {errors}");
        }

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            Permissions = dto.Permissions,
            UserCount = 0
        };
    }

    public async Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleDto dto)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role == null)
        {
            throw new Exception("Rol bulunamadı");
        }

        // Update fields
        if (dto.Description != null)
        {
            role.Description = dto.Description;
        }

        if (dto.Permissions != null)
        {
            role.Permissions = SerializePermissions(dto.Permissions);
        }

        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Rol güncellenemedi: {errors}");
        }

        return await GetRoleByIdAsync(roleId);
    }

    public async Task DeleteRoleAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());

        if (role == null)
        {
            throw new Exception("Rol bulunamadı");
        }

        // Check if role has users
        var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == roleId);
        if (userCount > 0)
        {
            throw new Exception($"Bu rol {userCount} kullanıcı tarafından kullanılıyor. Önce kullanıcılardan rolü kaldırın.");
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Rol silinemedi: {errors}");
        }
    }

    public async Task AssignRoleToUserAsync(AssignRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user == null || user.IsDeleted)
        {
            throw new Exception("Kullanıcı bulunamadı");
        }

        var roleExists = await _roleManager.RoleExistsAsync(dto.RoleName);
        if (!roleExists)
        {
            throw new Exception("Rol bulunamadı");
        }

        var isInRole = await _userManager.IsInRoleAsync(user, dto.RoleName);
        if (isInRole)
        {
            throw new Exception("Kullanıcı zaten bu role sahip");
        }

        var result = await _userManager.AddToRoleAsync(user, dto.RoleName);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Rol atanamadı: {errors}");
        }
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted)
        {
            throw new Exception("Kullanıcı bulunamadı");
        }

        var isInRole = await _userManager.IsInRoleAsync(user, roleName);
        if (!isInRole)
        {
            throw new Exception("Kullanıcı bu role sahip değil");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Rol kaldırılamadı: {errors}");
        }
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted)
        {
            throw new Exception("Kullanıcı bulunamadı");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    #region Private Methods

    private string? SerializePermissions(List<string>? permissions)
    {
        if (permissions == null || permissions.Count == 0)
            return null;

        return JsonSerializer.Serialize(permissions);
    }

    private List<string>? DeserializePermissions(string? permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(permissionsJson);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

