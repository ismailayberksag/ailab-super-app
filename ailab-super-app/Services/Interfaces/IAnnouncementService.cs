using System;
using System.Threading.Tasks;
using ailab_super_app.DTOs.Announcement;
using ailab_super_app.Helpers;

namespace ailab_super_app.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<Guid> CreateAsync(Guid actorUserId, CreateAnnouncementDto dto);
        Task MarkAsReadAsync(Guid announcementId, Guid userId);
        Task<PagedResult<AnnouncementListDto>> GetMyAnnouncementsAsync(Guid userId, PaginationParams pagination, bool? isRead = null);
        Task<AnnouncementDto> GetByIdAsync(Guid id, Guid requesterId);
    }
}
