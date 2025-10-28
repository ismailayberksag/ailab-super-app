using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ailab_super_app.Data;
using ailab_super_app.DTOs.Announcement;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using ailab_super_app.Common.Exceptions;

namespace ailab_super_app.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;

        public AnnouncementService(AppDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<Guid> CreateAsync(Guid actorUserId, CreateAnnouncementDto dto)
        {
            var isAdmin = await IsAdminAsync(actorUserId);

            if (dto.Scope == AnnouncementScope.Global)
            {
                if (!isAdmin)
                    throw new UnauthorizedAccessException("Global duyuru oluşturma yetkiniz yok.");
            }

            if (dto.Scope == AnnouncementScope.Project)
            {
                if (dto.TargetProjectIds == null || dto.TargetProjectIds.Count == 0)
                    throw new BadRequestException("Proje duyurusu için en az bir proje seçilmelidir.");

                if (!isAdmin)
                {
                    await ValidateCaptainProjectSelection(actorUserId, dto.TargetProjectIds);
                }
            }

            if (dto.Scope == AnnouncementScope.Individual)
            {
                if (dto.TargetUserIds == null || dto.TargetUserIds.Count == 0)
                    throw new BadRequestException("Bireysel duyuru için en az bir kullanıcı seçilmelidir.");

                if (!isAdmin)
                {
                    // Captain birden fazla projede olabilir; hangi projeler kapsamında gönderdiğini belirtmeli
                    if (dto.TargetProjectIds == null || dto.TargetProjectIds.Count == 0)
                        throw new BadRequestException("Captain olarak bireysel duyuru gönderirken kapsayan projeleri seçmelisiniz.");

                    await ValidateCaptainProjectSelection(actorUserId, dto.TargetProjectIds);
                    await ValidateUsersBelongToSelectedProjects(dto.TargetUserIds, dto.TargetProjectIds);
                }
            }

            var entity = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Content = dto.Content,
                Scope = dto.Scope,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorUserId
            };

            if (dto.Scope == AnnouncementScope.Project && dto.TargetProjectIds != null)
            {
                entity.TargetProjects = dto.TargetProjectIds
                    .Distinct()
                    .Select(pid => new AnnouncementProject
                    {
                        AnnouncementId = entity.Id,
                        ProjectId = pid
                    }).ToList();
            }

            if (dto.Scope == AnnouncementScope.Individual && dto.TargetUserIds != null)
            {
                entity.TargetUsers = dto.TargetUserIds
                    .Distinct()
                    .Select(uid => new AnnouncementUser
                    {
                        AnnouncementId = entity.Id,
                        UserId = uid,
                        IsRead = false
                    }).ToList();

                // Captain için seçilen projeler sadece kontrol amaçlı kullanıldı; DB'ye ayrıca yazılmasına gerek yok
                if (isAdmin == false && dto.TargetProjectIds != null && dto.TargetProjectIds.Count > 0)
                {
                    // İsteğe göre: Bireysel duyuruda da projeleri ilişkilendirmek istenirse aşağıyı açabilirsiniz.
                    // entity.TargetProjects = dto.TargetProjectIds.Distinct().Select(pid => new AnnouncementProject
                    // {
                    //     AnnouncementId = entity.Id,
                    //     ProjectId = pid
                    // }).ToList();
                }
            }

            _db.Announcements.Add(entity);
            await _db.SaveChangesAsync();

            return entity.Id;
        }

        public async Task MarkAsReadAsync(Guid announcementId, Guid userId)
        {
            // Erişim kontrolü: kullanıcı bu duyuruyu görebiliyor mu?
            var canAccess = await UserCanAccessAnnouncementAsync(announcementId, userId);
            if (!canAccess)
                throw new UnauthorizedAccessException("Bu duyuruya erişim yetkiniz yok.");

            var au = await _db.AnnouncementUsers
                .FirstOrDefaultAsync(x => x.AnnouncementId == announcementId && x.UserId == userId);

            if (au == null)
            {
                _db.AnnouncementUsers.Add(new AnnouncementUser
                {
                    AnnouncementId = announcementId,
                    UserId = userId,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow
                });
            }
            else
            {
                if (!au.IsRead)
                {
                    au.IsRead = true;
                    au.ReadAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<PagedResult<AnnouncementListDto>> GetMyAnnouncementsAsync(Guid userId, PaginationParams pagination, bool? isRead = null)
        {
            // Kullanıcının üye olduğu (silinmemiş) projeler
            var userProjectIds = await _db.ProjectMembers
                .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            var baseQuery = _db.Announcements.AsNoTracking()
                .Where(a =>
                    a.Scope == AnnouncementScope.Global
                    || (a.Scope == AnnouncementScope.Project && a.TargetProjects.Any(tp => userProjectIds.Contains(tp.ProjectId)))
                    || (a.Scope == AnnouncementScope.Individual && a.TargetUsers.Any(tu => tu.UserId == userId))
                );

            // Kullanıcıya göre okunma durumu
            var queryWithRead = baseQuery
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Scope,
                    a.CreatedAt,
                    IsRead = a.TargetUsers.Any(tu => tu.UserId == userId && tu.IsRead),
                });

            if (isRead.HasValue)
            {
                queryWithRead = queryWithRead.Where(x => x.IsRead == isRead.Value);
            }

            var totalCount = await queryWithRead.CountAsync();

            var items = await queryWithRead
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(x => new AnnouncementListDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Scope = x.Scope,
                    CreatedAt = x.CreatedAt,
                    IsRead = x.IsRead,
                    // İsteğe bağlı kısa önizleme:
                    Preview = null
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

        public async Task<AnnouncementDto> GetByIdAsync(Guid id, Guid requesterId)
        {
            // Erişim kontrolü
            var canAccess = await UserCanAccessAnnouncementAsync(id, requesterId);
            if (!canAccess)
                throw new UnauthorizedAccessException("Bu duyuruya erişim yetkiniz yok.");

            // Temel duyuru
            var a = await _db.Announcements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) throw new NotFoundException("Duyuru bulunamadı.");

            // Oluşturan kullanıcı
            var creator = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == a.CreatedBy);

            var dto = new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                Scope = a.Scope,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                CreatedByName = creator?.FullName,
                CreatedByEmail = creator?.Email
            };

            if (a.Scope == AnnouncementScope.Project)
            {
                // Proje metrikleri
                dto.AnnouncementProjects = await _db.AnnouncementProjects
                    .Where(ap => ap.AnnouncementId == a.Id)
                    .Select(ap => new TargetProjectInfo
                    {
                        ProjectId = ap.ProjectId,
                        ProjectName = _db.Projects.Where(p => p.Id == ap.ProjectId).Select(p => p.Name).FirstOrDefault()!,
                        MemberCount = _db.ProjectMembers.Count(pm => pm.ProjectId == ap.ProjectId && !pm.IsDeleted),
                        ReadCount = (
                            from pm in _db.ProjectMembers
                            join au in _db.AnnouncementUsers on pm.UserId equals au.UserId
                            where pm.ProjectId == ap.ProjectId
                                  && !pm.IsDeleted
                                  && au.AnnouncementId == a.Id
                                  && au.IsRead
                            select au.UserId
                        ).Distinct().Count()
                    })
                    .ToListAsync();
            }
            else if (a.Scope == AnnouncementScope.Individual)
            {
                // Bireysel alıcı listesi
                dto.AnnouncementUsers = await _db.AnnouncementUsers
                    .Where(au => au.AnnouncementId == a.Id)
                    .Select(au => new AnnouncementRecipientDto
                    {
                        UserId = au.UserId,
                        FullName = _db.Users.Where(u => u.Id == au.UserId).Select(u => u.FullName ?? "").FirstOrDefault()!,
                        Email = _db.Users.Where(u => u.Id == au.UserId).Select(u => u.Email ?? "").FirstOrDefault()!,
                        IsRead = au.IsRead,
                        ReadAt = au.ReadAt
                    })
                    .ToListAsync();
            }

            return dto;
        }

        // ---------- Private helpers ----------

        private async Task<bool> IsAdminAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, "Admin");
        }

        private async Task<List<Guid>> GetCaptainProjectIdsAsync(Guid userId)
        {
            return await _db.ProjectMembers
                .Where(pm => pm.UserId == userId && pm.Role == "Captain" && !pm.IsDeleted)
                .Select(pm => pm.ProjectId)
                .ToListAsync();
        }

        private async Task ValidateCaptainProjectSelection(Guid userId, IEnumerable<Guid> selectedProjectIds)
        {
            var captainProjects = await GetCaptainProjectIdsAsync(userId);
            var selected = selectedProjectIds.Distinct().ToList();

            var invalid = selected.Except(captainProjects).ToList();
            if (invalid.Count > 0)
                throw new UnauthorizedAccessException("Captain yalnızca kaptanı olduğu projeler için duyuru oluşturabilir.");
        }

        private async Task ValidateUsersBelongToSelectedProjects(IEnumerable<Guid> targetUserIds, IEnumerable<Guid> selectedProjectIds)
        {
            var selected = selectedProjectIds.Distinct().ToList();
            var targets = targetUserIds.Distinct().ToList();

            var memberships = await _db.ProjectMembers
                .Where(pm => targets.Contains(pm.UserId) && selected.Contains(pm.ProjectId) && !pm.IsDeleted)
                .Select(pm => new { pm.UserId, pm.ProjectId })
                .ToListAsync();

            foreach (var uid in targets)
            {
                var userHasAny = memberships.Any(m => m.UserId == uid);
                if (!userHasAny)
                    throw new BadRequestException("Seçilen bazı kullanıcılar belirtilen projelerin üyesi değil.");
            }
        }

        private async Task<bool> UserCanAccessAnnouncementAsync(Guid announcementId, Guid userId)
        {
            var userProjectIds = await _db.ProjectMembers
                .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            return await _db.Announcements.AsNoTracking()
                .Where(a => a.Id == announcementId)
                .AnyAsync(a =>
                    a.Scope == AnnouncementScope.Global
                    || (a.Scope == AnnouncementScope.Project && a.TargetProjects.Any(tp => userProjectIds.Contains(tp.ProjectId)))
                    || (a.Scope == AnnouncementScope.Individual && a.TargetUsers.Any(tu => tu.UserId == userId))
                );
        }
    }
}