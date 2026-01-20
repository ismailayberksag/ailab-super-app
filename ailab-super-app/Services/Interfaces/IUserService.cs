using ailab_super_app.DTOs.Statistics;
using ailab_super_app.DTOs.User;
using ailab_super_app.Helpers;

namespace ailab_super_app.Services.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserListDto>> GetUsersAsync(PaginationParams paginationParams);
        Task<UserDto> GetUserByIdAsync(Guid userId);
            Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto);
            Task<UserDto> UpdateUserEmailAsync(Guid userId, string newEmail); // Eklendi
            Task<UserDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusDto dto);        Task DeleteUserAsync(Guid userId, Guid deletedBy);
        Task<List<LeaderboardUserDto>> GetTopUsersAsync(int count);
    }
}