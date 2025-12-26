using ailab_super_app.DTOs.BugReport;

namespace ailab_super_app.Services.Interfaces;

public interface IBugReportService
{
    Task<Guid> CreateReportAsync(Guid userId, CreateBugReportDto dto);
}
