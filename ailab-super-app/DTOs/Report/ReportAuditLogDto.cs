namespace ailab_super_app.DTOs.Report;

public class ReportAuditLogDto
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string Action { get; set; } = default!;
    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Comment { get; set; }
}