using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ailab_super_app.Data;
using ailab_super_app.DTOs.Announcement;
using ailab_super_app.Helpers; 
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;

        public AnnouncementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateAsync(Guid actorUserId, CreateAnnouncementDto dto)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Content = dto.Content,
                Scope = dto.Scope,
                CreatedBy = actorUserId,
                CreatedAt = now
            };

            if (dto.Scope == AnnouncementScope.Project && dto.TargetProjectIds != null)
            {
                foreach (var projectId in dto.TargetProjectIds)
                {
                    announcement.TargetProjects.Add(new AnnouncementProject
                    {
                        AnnouncementId = announcement.Id,
                        ProjectId = projectId
                    });
                }
            }
            else if (dto.Scope == AnnouncementScope.Individual && dto.TargetUserIds != null)
            {
                foreach (var userId in dto.TargetUserIds)
                {
                    announcement.TargetUsers.Add(new AnnouncementUser
                    {
                        AnnouncementId = announcement.Id,
                        UserId = userId,
                        IsRead = false
                    });
                }
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return announcement.Id;
        }

        public async Task<PagedResult<AnnouncementListDto>> GetMyAnnouncementsAsync(Guid userId, PaginationParams pagination, bool? isRead = null)
        {
            var userProjectIds = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            var query = _context.Announcements
                .AsNoTracking()
                .Where(a => !a.IsDeleted && (
                    a.Scope == AnnouncementScope.Global ||
                    (a.Scope == AnnouncementScope.Project && a.TargetProjects.Any(tp => userProjectIds.Contains(tp.ProjectId))) ||
                    (a.Scope == AnnouncementScope.Individual && a.TargetUsers.Any(tu => tu.UserId == userId))
                ));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(a => new AnnouncementListDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    CreatedAt = a.CreatedAt,
                    Scope = a.Scope,
                    IsRead = a.Scope == AnnouncementScope.Individual 
                        ? a.TargetUsers.First(tu => tu.UserId == userId).IsRead 
                        : false 
                })
                .ToListAsync();

            return new PagedResult<AnnouncementListDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }

        public async Task<AnnouncementDto> GetByIdAsync(Guid id, Guid userId)
        {
            var announcement = await _context.Announcements
                .Include(a => a.TargetProjects)
                .Include(a => a.TargetUsers)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (announcement == null) throw new Exception("Duyuru bulunamadı.");

            return new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                CreatedAt = announcement.CreatedAt,
                Scope = announcement.Scope
            };
        }

        public async Task MarkAsReadAsync(Guid id, Guid userId)
        {
            var targetUser = await _context.AnnouncementUsers
                .FirstOrDefaultAsync(tu => tu.AnnouncementId == id && tu.UserId == userId);

            if (targetUser != null)
            {
                targetUser.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
