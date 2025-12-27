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
            // Kullanıcı ve Rol Kontrolü
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.ProjectMemberships) // Eklendi
                .FirstOrDefaultAsync(u => u.Id == actorUserId);

            if (user == null) throw new Exception("Kullanıcı bulunamadı.");

            bool isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
            bool isCaptain = user.ProjectMemberships.Any(pm => pm.Role == "Captain"); // Genel kaptanlık kontrolü

            // 1. Member Kontrolü (Hiçbir şey yapamaz)
            if (!isAdmin && !isCaptain)
            {
                throw new UnauthorizedAccessException("Duyuru oluşturma yetkiniz yok.");
            }

            // 2. Global Duyuru Kontrolü (Sadece Admin)
            if (dto.Scope == AnnouncementScope.Global && !isAdmin)
            {
                throw new UnauthorizedAccessException("Global duyuru sadece Admin tarafından oluşturulabilir.");
            }

            // 3. Proje Duyurusu Kontrolü (Captain Sadece Kendi Projesine)
            if (dto.Scope == AnnouncementScope.Project && !isAdmin)
            {
                // Kaptan, hedef projelerin hepsinde kaptan mı?
                if (dto.TargetProjectIds == null || !dto.TargetProjectIds.Any())
                {
                    throw new Exception("Hedef proje seçilmelidir.");
                }

                var captainProjectIds = await _context.ProjectMembers
                    .Where(pm => pm.UserId == actorUserId && pm.Role == "Captain" && !pm.IsDeleted)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

                bool allTargetsAreManaged = dto.TargetProjectIds.All(id => captainProjectIds.Contains(id));

                if (!allTargetsAreManaged)
                {
                    throw new UnauthorizedAccessException("Sadece kaptanı olduğunuz projelere duyuru atabilirsiniz.");
                }
            }
            
            // 4. Bireysel Duyuru Kontrolü (Captain Sadece Kendi Projesindeki Üyelere)
            if (dto.Scope == AnnouncementScope.Individual && !isAdmin)
            {
                 // Bu mantık biraz karmaşık olabilir, kaptanın tüm projelerindeki üyeleri çekip kontrol etmek gerekir.
                 // Şimdilik Captain bireysel atabilir varsayımıyla devam ediyorum (Raporda 'Seçili üyelere' yetkisi var).
                 // Ancak güvenlik için: Hedef kullanıcı, kaptanın herhangi bir projesinde üye mi?
                 
                 var captainProjectIds = await _context.ProjectMembers
                    .Where(pm => pm.UserId == actorUserId && pm.Role == "Captain" && !pm.IsDeleted)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();
                 
                 var memberIdsInManagedProjects = await _context.ProjectMembers
                    .Where(pm => captainProjectIds.Contains(pm.ProjectId) && !pm.IsDeleted)
                    .Select(pm => pm.UserId)
                    .ToListAsync();

                 if (dto.TargetUserIds != null && !dto.TargetUserIds.All(id => memberIdsInManagedProjects.Contains(id)))
                 {
                     throw new UnauthorizedAccessException("Sadece projelerinizdeki üyelere duyuru atabilirsiniz.");
                 }
            }

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
            // Kullanıcının dahil olduğu projeleri bul
            var userProjectIds = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            // Kullanıcının okuduğu duyuruların ID'lerini çek (Performans için)
            var readAnnouncementIds = await _context.AnnouncementUsers
                .Where(au => au.UserId == userId && au.IsRead)
                .Select(au => au.AnnouncementId)
                .ToListAsync();

            var query = _context.Announcements
                .AsNoTracking()
                .Where(a => !a.IsDeleted && (
                    a.Scope == AnnouncementScope.Global ||
                    (a.Scope == AnnouncementScope.Project && a.TargetProjects.Any(tp => userProjectIds.Contains(tp.ProjectId))) ||
                    (a.Scope == AnnouncementScope.Individual && a.TargetUsers.Any(tu => tu.UserId == userId))
                ));

            // Filtreleme: isRead parametresi varsa uygula
            if (isRead.HasValue)
            {
                if (isRead.Value)
                {
                    // Sadece okunanları getir
                    query = query.Where(a => readAnnouncementIds.Contains(a.Id));
                }
                else
                {
                    // Sadece okunmayanları getir
                    query = query.Where(a => !readAnnouncementIds.Contains(a.Id));
                }
            }

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
                    // Okunma durumu kontrolü: Listede var mı?
                    IsRead = readAnnouncementIds.Contains(a.Id)
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

            // Erişim Kontrolü
            bool hasAccess = false;

            if (announcement.Scope == AnnouncementScope.Global)
            {
                hasAccess = true;
            }
            else if (announcement.Scope == AnnouncementScope.Project)
            {
                // Kullanıcının proje üyeliklerini kontrol et
                var userProjectIds = await _context.ProjectMembers
                    .Where(pm => pm.UserId == userId && !pm.IsDeleted)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

                hasAccess = announcement.TargetProjects.Any(tp => userProjectIds.Contains(tp.ProjectId));
            }
            else if (announcement.Scope == AnnouncementScope.Individual)
            {
                hasAccess = announcement.TargetUsers.Any(tu => tu.UserId == userId);
            }

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("Bu duyuruya erişim yetkiniz yok.");
            }

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
                // Zaten kayıt varsa güncelle (Idempotent)
                if (!targetUser.IsRead)
                {
                    targetUser.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Kayıt yoksa (Global/Project duyurusu ilk kez okunuyor), yeni kayıt oluştur (INSERT)
                _context.AnnouncementUsers.Add(new AnnouncementUser
                {
                    AnnouncementId = id,
                    UserId = userId,
                    IsRead = true
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}
