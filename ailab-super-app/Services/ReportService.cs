using ailab_super_app.Data;
using ailab_super_app.DTOs.Report;
using ailab_super_app.Helpers;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly FirebaseStorageService _firebaseService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(AppDbContext context, FirebaseStorageService firebaseService, ILogger<ReportService> logger)
        {
            _context = context;
            _firebaseService = firebaseService;
            _logger = logger;
        }

        public async Task<ReportRequestDto> CreateRequestAsync(Guid actorId, CreateReportRequestDto dto)
        {
            var request = new ReportRequest
            {
                Id = Guid.NewGuid(),
                CreatedBy = actorId,
                Title = dto.Title,
                Description = dto.Description,
                RequestedAt = DateTime.UtcNow,
                DueDate = dto.DueDate?.ToUniversalTime(),
                PeriodType = dto.PeriodType,
                PeriodStart = dto.PeriodStart?.ToUniversalTime(),
                PeriodEnd = dto.PeriodEnd?.ToUniversalTime(),
                Status = ReportRequestStatus.Pending
            };

            var targetProjectIds = new List<Guid>();

            if (dto.TargetAllProjects)
            {
                targetProjectIds = await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .Select(p => p.Id)
                    .ToListAsync();
            }
            else if (dto.TargetProjectIds != null && dto.TargetProjectIds.Any())
            {
                targetProjectIds = dto.TargetProjectIds;
            }
            else
            {
                throw new ArgumentException("En az bir proje seçilmeli veya 'Tüm Projeler' işaretlenmelidir.");
            }

            targetProjectIds = targetProjectIds.Distinct().ToList();
            
            foreach (var projectId in targetProjectIds)
            {
                request.TargetProjects.Add(new ReportRequestProject
                {
                    Id = Guid.NewGuid(),
                    ReportRequestId = request.Id,
                    ProjectId = projectId,
                    PenaltyApplied = false
                });
            }

            _context.ReportRequests.Add(request);
            await _context.SaveChangesAsync();

            return await GetRequestByIdAsync(request.Id, actorId);
        }

        public async Task<ReportRequestDto> GetRequestByIdAsync(Guid requestId, Guid actorId)
        {
            var request = await _context.ReportRequests
                .Include(r => r.CreatedByUser)
                .Include(r => r.TargetProjects)
                    .ThenInclude(rp => rp.Project)
                .Include(r => r.SubmittedReports)
                    .ThenInclude(sr => sr.Project)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new KeyNotFoundException("Rapor talebi bulunamadı.");

            var dto = new ReportRequestDto
            {
                Id = request.Id,
                CreatedBy = request.CreatedBy,
                CreatedByName = request.CreatedByUser?.FullName ?? "Unknown",
                Title = request.Title,
                Description = request.Description,
                RequestedAt = request.RequestedAt,
                DueDate = request.DueDate,
                PeriodType = request.PeriodType,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Status = request.Status,
                TargetProjects = request.TargetProjects.Select(tp => new TargetProjectDto
                {
                    ProjectId = tp.ProjectId,
                    ProjectName = tp.Project.Name,
                    PenaltyApplied = tp.PenaltyApplied,
                    HasSubmitted = request.SubmittedReports.Any(sr => sr.ProjectId == tp.ProjectId && sr.IsActive && sr.Status != ReportStatus.Rejected)
                }).ToList()
            };

            return dto;
        }

        public async Task<PagedResult<ReportRequestDto>> GetMyAssignedRequestsAsync(Guid actorId, PaginationParams pagination)
        {
            var userProjectIds = await _context.ProjectMembers
                .Where(pm => pm.UserId == actorId && !pm.IsDeleted)
                .Select(pm => pm.ProjectId)
                .ToListAsync();

            if (!userProjectIds.Any())
                return new PagedResult<ReportRequestDto>
                {
                    Items = new List<ReportRequestDto>(),
                    TotalCount = 0,
                    PageNumber = pagination.PageNumber,
                    PageSize = pagination.PageSize
                };

            var query = _context.ReportRequests
                .Include(r => r.CreatedByUser)
                .Include(r => r.TargetProjects)
                    .ThenInclude(tp => tp.Project)
                .Where(r => !r.IsDeleted && r.TargetProjects.Any(tp => userProjectIds.Contains(tp.ProjectId)))
                .OrderByDescending(r => r.CreatedByUser);

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(r => new ReportRequestDto
                {
                    Id = r.Id,
                    CreatedBy = r.CreatedBy,
                    CreatedByName = r.CreatedByUser.FullName,
                    Title = r.Title,
                    Description = r.Description,
                    DueDate = r.DueDate,
                    Status = r.Status,
                    TargetProjects = r.TargetProjects
                        .Where(tp => userProjectIds.Contains(tp.ProjectId))
                        .Select(tp => new TargetProjectDto
                        {
                            ProjectId = tp.ProjectId,
                            ProjectName = tp.Project.Name,
                            PenaltyApplied = tp.PenaltyApplied
                        }).ToList()
                })
                .ToListAsync();

            return new PagedResult<ReportRequestDto>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }
        
        public async Task<PagedResult<ReportRequestDto>> GetMyCreatedRequestsAsync(Guid actorId, PaginationParams pagination)
        {
             var query = _context.ReportRequests
                .Where(r => r.CreatedBy == actorId && !r.IsDeleted)
                .OrderByDescending(r => r.RequestedAt);

            var totalItems = await query.CountAsync();
            
             var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(r => new ReportRequestDto {
                    Id = r.Id,
                    Title = r.Title,
                    Status = r.Status,
                    DueDate = r.DueDate
                })
                .ToListAsync();

            return new PagedResult<ReportRequestDto>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }

        public async Task<PagedResult<ReportRequestDto>> GetProjectRequestsAsync(Guid projectId, Guid actorId, PaginationParams pagination)
        {
             var query = _context.ReportRequests
                .Where(r => r.TargetProjects.Any(tp => tp.ProjectId == projectId) && !r.IsDeleted)
                .OrderByDescending(r => r.RequestedAt);
            
             var totalItems = await query.CountAsync();
             var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(r => new ReportRequestDto {
                    Id = r.Id,
                    Title = r.Title,
                    DueDate = r.DueDate,
                    Status = r.Status
                })
                .ToListAsync();

             return new PagedResult<ReportRequestDto>
            {
                Items = items,
                TotalCount = totalItems,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }

        public async Task<ReportDto> UploadReportAsync(Guid actorId, UploadReportDto dto)
        {
            var request = await _context.ReportRequests
                .Include(r => r.TargetProjects)
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId);
                
            if (request == null) throw new KeyNotFoundException("Rapor talebi bulunamadı.");

            var userMembership = await _context.ProjectMembers
                .Where(pm => pm.UserId == actorId && !pm.IsDeleted && pm.Role == "Captain")
                .ToListAsync();

            var targetProjectIds = request.TargetProjects.Select(tp => tp.ProjectId).ToList();
            var validProject = userMembership.FirstOrDefault(pm => targetProjectIds.Contains(pm.ProjectId));

            if (validProject == null)
            {
                throw new UnauthorizedAccessException("Bu rapor talebi için yükleme yapma yetkiniz (Captain) yok veya proje hedefte değil.");
            }

            var projectId = validProject.ProjectId;

            var activeReports = await _context.Reports
                .Where(r => r.RequestId == dto.RequestId && r.ProjectId == projectId && r.IsActive)
                .ToListAsync();

            foreach (var r in activeReports)
            {
                r.IsActive = false;
            }

            string remotePath = $"reports/{projectId}/{dto.RequestId}/{Guid.NewGuid()}_{dto.PdfFile.FileName}";
            string savedPath = "";
            try {
                savedPath = await _firebaseService.UploadFileAsync(dto.PdfFile, remotePath);
            }
            catch (Exception ex)
            {
                throw new Exception("Dosya yüklenirken hata oluştu: " + ex.Message);
            }

            var newReport = new Report
            {
                Id = Guid.NewGuid(),
                RequestId = dto.RequestId,
                ProjectId = projectId,
                SubmittedBy = actorId,
                Title = dto.Title,
                Description = dto.Description,
                FilePath = savedPath,
                SubmittedAt = DateTime.UtcNow,
                PeriodType = dto.PeriodType,
                PeriodStart = dto.PeriodStart?.ToUniversalTime(),
                PeriodEnd = dto.PeriodEnd?.ToUniversalTime(),
                Status = ReportStatus.Submitted,
                IsActive = true
            };

            try {
                _context.Reports.Add(newReport);
                
                _context.ReportAuditLogs.Add(new ReportAuditLog {
                    Id = Guid.NewGuid(),
                    ReportId = newReport.Id,
                    Action = "Uploaded",
                    PerformedByUserId = actorId,
                    Timestamp = DateTime.UtcNow,
                    Comment = "New version uploaded."
                });

                await _context.SaveChangesAsync();
            }
            catch(Exception)
            {
                await _firebaseService.DeleteFileAsync(savedPath);
                throw;
            }

            return await GetReportByIdAsync(newReport.Id, actorId);
        }

        public async Task<ReportDto> GetReportByIdAsync(Guid reportId, Guid actorId)
        {
            var report = await _context.Reports
                .Include(r => r.Project)
                .Include(r => r.ReportRequest)
                .FirstOrDefaultAsync(r => r.Id == reportId);
                
            if (report == null) throw new KeyNotFoundException("Rapor bulunamadı.");

            return new ReportDto {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                FilePath = report.FilePath,
                Status = report.Status,
                ProjectId = report.ProjectId,
                ProjectName = report.Project.Name,
                RequestId = report.RequestId,
                SubmittedAt = report.SubmittedAt,
                IsActive = report.IsActive,
                ReviewNotes = report.ReviewNotes,
                ReviewedAt = report.ReviewedAt
            };
        }
        
        public async Task<PagedResult<ReportDto>> GetReportsAsync(Guid actorId, ReportFilterDto filter, PaginationParams pagination)
        {
             return new PagedResult<ReportDto>
             {
                 Items = new List<ReportDto>(),
                 TotalCount = 0,
                 PageNumber = 1,
                 PageSize = 10
             };
        }

        public async Task<string> GetSignedDownloadUrlAsync(Guid reportId, Guid actorId)
        {
             var report = await _context.Reports.FindAsync(reportId);
             if (report == null) throw new KeyNotFoundException();
             return await _firebaseService.GetSignedUrlAsync(report.FilePath);
        }

        public async Task<bool> CanReviewReportAsync(Guid reportId, Guid actorId)
        {
            return true;
        }

        public async Task<ReportDto> ReviewReportAsync(Guid reportId, Guid actorId, ReviewReportDto dto)
        {
             var report = await _context.Reports.FindAsync(reportId);
             if (report == null) throw new KeyNotFoundException();

             if (dto.Status != ReportStatus.Approved && dto.Status != ReportStatus.Rejected)
             {
                 throw new ArgumentException("Rapor durumu sadece Approved veya Rejected olabilir.");
             }

             if (dto.Status == ReportStatus.Rejected && string.IsNullOrWhiteSpace(dto.Reason))
             {
                 throw new ArgumentException("Reddedilen raporlar için sebep belirtilmelidir.");
             }

             report.Status = dto.Status;
             report.ReviewedBy = actorId;
             report.ReviewedAt = DateTime.UtcNow;
             report.ReviewNotes = dto.Reason;

             _context.ReportAuditLogs.Add(new ReportAuditLog {
                 Id = Guid.NewGuid(),
                 ReportId = report.Id,
                 Action = report.Status.ToString(),
                 PerformedByUserId = actorId,
                 Timestamp = DateTime.UtcNow,
                 Comment = dto.Reason
             });

             await _context.SaveChangesAsync();
             return await GetReportByIdAsync(reportId, actorId);
        }

        public Task<ReportRequestDto> UpdateRequestAsync(Guid requestId, Guid actorId, UpdateReportRequestDto dto)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRequestAsync(Guid requestId, Guid actorId)
        {
            throw new NotImplementedException();
        }
    }
}