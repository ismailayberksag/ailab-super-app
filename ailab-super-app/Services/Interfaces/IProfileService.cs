using ailab_super_app.DTOs.User;

namespace ailab_super_app.Services.Interfaces
{
    public interface IProfileService
    {
        Task UpdateProfileImageAsync(Guid userId, UpdateProfileImageDto dto);
        DefaultAvatarListDto GetDefaultAvatars();
    }
}
