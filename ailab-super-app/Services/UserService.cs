using ailab_super_app.Data;
using ailab_super_app.DTOs.Statistics;
using ailab_super_app.DTOs.User;
using ailab_super_app.Helpers; // GetTurkeyTime için eklendi
using ailab_super_app.Models;
using ailab_super_app.Models.Enums; // Eklendi
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
                    ProfileImageUrl = user.ProfileImageUrl,
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
                UserName = user.UserName!,
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
            var now = DateTimeHelper.GetTurkeyTime();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            if (dto.Phone != null)
            {
                user.Phone = dto.Phone;
                user.PhoneNumber = dto.Phone;
            }

            user.UpdatedAt = now;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı güncellenemedi: {errors}");
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserDto> UpdateUserEmailAsync(Guid userId, string newEmail)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            // Email ve UserName senkronizasyonu
            user.Email = newEmail;
            user.UserName = newEmail; // Bizim sistemde UserName email ile aynı tutuluyor genelde
            user.NormalizedEmail = newEmail.ToUpperInvariant();
            user.NormalizedUserName = newEmail.ToUpperInvariant();
            user.UpdatedAt = now;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"E-posta güncellenemedi: {errors}");
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            user.Status = dto.Status;
            user.UpdatedAt = now;

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
            var now = DateTimeHelper.GetTurkeyTime();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            user.IsDeleted = true;
            user.DeletedAt = now;
            user.DeletedBy = deletedBy;
            user.UpdatedAt = now;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı silinemedi: {errors}");
            }
        }

        public async Task<List<LeaderboardUserDto>> GetTopUsersAsync(int count)
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderByDescending(u => u.TotalScore)
                .ThenBy(u => u.FullName)
                .Take(count)
                .Select(u => new LeaderboardUserDto
                {
                    FullName = u.FullName ?? u.UserName ?? "Bilinmeyen Kullanıcı",
                    TotalScore = u.TotalScore,
                    ProfileImageUrl = u.ProfileImageUrl
                })
                .ToListAsync();

            return users;
        }
    }
}