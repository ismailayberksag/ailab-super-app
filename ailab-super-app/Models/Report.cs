using ailab_super_app.Models.Enums;

namespace ailab_super_app.Models;

public class Report
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string FilePath { get; set; } = default!;

    public string? PeriodType { get; set; }

    public DateTime? PeriodStart { get; set; }

    public DateTime? PeriodEnd { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid? RequestId { get; set; }
    public ReportRequest? Request { get; set; }

    public Guid? SubmittedBy { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNotes { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}