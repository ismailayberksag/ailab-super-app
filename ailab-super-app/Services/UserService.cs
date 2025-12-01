using ailab_super_app.Data;
using ailab_super_app.DTOs.User;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ailab_super_app.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<User> userManager,
            AppDbContext context,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<UserListDto>> GetUsersAsync(PaginationParams paginationParams)
        {
            var query = _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var userListDtos = new List<UserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userListDtos.Add(new UserListDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    Status = user.Status,
                    TotalScore = user.TotalScore,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }

            return new PagedResult<UserListDto>
            {
                Items = userListDtos,
                TotalCount = totalCount,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            };
        }

        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                SchoolNumber = user.SchoolNumber,
                FullName = user.FullName,
                Phone = user.Phone,
                ProfileImageUrl = user.ProfileImageUrl,
                Status = user.Status,
                TotalScore = user.TotalScore,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = roles.ToList()
            };
        }

        public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            // Update fields
            if (dto.Phone != null)
            {
                user.Phone = dto.Phone;
                user.PhoneNumber = dto.Phone; // Keep Identity PhoneNumber in sync
            }
            // AvatarUrl update logic removed

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı güncellenemedi: {errors}");
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            user.Status = dto.Status;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı durumu güncellenemedi: {errors}");
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task DeleteUserAsync(Guid userId, Guid deletedBy)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = deletedBy;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı silinemedi: {errors}");
            }
        }
        
        public async Task<UserDto> UpdateAvatarAsync(Guid userId, string avatarUrl)
        {
            var user = await _userManager.Users.Include(u => u.UserAvatar).FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                throw new ArgumentException("Avatar URL'i boş olamaz.");
            }
            
            // Yeni avatar kaydını avatars tablosuna ekle
            var newAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Name = $"user_{userId}_custom_{DateTime.UtcNow.Ticks}", // Benzersiz isim
                StoragePath = avatarUrl,
                PublicUrl = avatarUrl, // Frontend'den gelen URL'i direkt kullan
                IsSystemDefault = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Avatars.Add(newAvatar);

            // Kullanıcının UserAvatar kaydını güncelle veya oluştur
            if (user.UserAvatar != null)
            {
                user.UserAvatar.AvatarId = newAvatar.Id;
                user.UserAvatar.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                user.UserAvatar = new UserAvatar
                {
                    UserId = userId,
                    AvatarId = newAvatar.Id,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserDto> SetSystemAvatarAsync(Guid userId, Guid avatarId)
        {
            var user = await _userManager.Users.Include(u => u.UserAvatar).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            var systemAvatar = await _context.Avatars.FirstOrDefaultAsync(a => a.Id == avatarId && a.IsSystemDefault);
            if (systemAvatar == null)
            {
                throw new Exception("Sistem avatarı bulunamadı veya geçerli değil.");
            }

            // Kullanıcının UserAvatar kaydını güncelle veya oluştur
            if (user.UserAvatar != null)
            {
                user.UserAvatar.AvatarId = systemAvatar.Id;
                user.UserAvatar.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                user.UserAvatar = new UserAvatar
                {
                    UserId = userId,
                    AvatarId = systemAvatar.Id,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(userId);
        }

        public async Task<string> GetAvatarAsync(Guid userId)
        {
            var avatarUrl = await _context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => u.UserAvatar!.Avatar.PublicUrl) // PublicUrl'i döndür
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(avatarUrl))
            {
                // Kullanıcının avatarı yoksa veya silinmişse, varsayılan sistem avatarını döndür
                var defaultAvatar = await _context.Avatars.FirstOrDefaultAsync(a => a.IsSystemDefault);
                if (defaultAvatar != null)
                {
                    return defaultAvatar.PublicUrl;
                }
                // Hiçbir varsayılan avatar da yoksa bir fallback URL döndür
                return "https://default-fallback-avatar.com/default.png"; // Placeholder
            }
            return avatarUrl;
        }
    }
}