using ailab_super_app.DTOs.Report;
using ailab_super_app.Helpers;

namespace ailab_super_app.Services.Interfaces;

public interface IReportService
{
    Task<ReportRequestDto> CreateRequestAsync(Guid actorId, CreateReportRequestDto dto);
    Task<ReportRequestDto> UpdateRequestAsync(Guid requestId, Guid actorId, UpdateReportRequestDto dto);
    Task DeleteRequestAsync(Guid requestId, Guid actorId);
    Task<ReportRequestDto> GetRequestByIdAsync(Guid requestId, Guid actorId);

    // OLUŞTURDUKLARIM (RequestedBy = actorId)
    Task<PagedResult<ReportRequestDto>> GetMyCreatedRequestsAsync(Guid actorId, PaginationParams pagination);

    // BANA ATANANLAR (TargetUsers veya proje üyelikleri)
    Task<PagedResult<ReportRequestDto>> GetMyAssignedRequestsAsync(Guid actorId, PaginationParams pagination);

    // Belirli projenin talepleri (Admin veya proje üyesi)
    Task<PagedResult<ReportRequestDto>> GetProjectRequestsAsync(Guid projectId, Guid actorId, PaginationParams pagination);

    Task<ReportDto> UploadReportAsync(Guid actorId, UploadReportDto dto);
    Task<PagedResult<ReportDto>> GetReportsAsync(Guid actorId, ReportFilterDto filter, PaginationParams pagination);
    Task<ReportDto> GetReportByIdAsync(Guid reportId, Guid actorId);

    // downlaod
    Task<string> GetSignedDownloadUrlAsync(Guid reportId, Guid actorId);

    // review
    Task<bool> CanReviewReportAsync(Guid reportId, Guid actorId);
    Task<ReportDto> ReviewReportAsync(Guid reportId, Guid actorId, ReviewReportDto dto);
}