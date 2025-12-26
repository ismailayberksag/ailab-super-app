using ailab_super_app.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

public class Report
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;
    public string FilePath { get; set; } = default!;

    public PeriodType? PeriodType { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public DateTime SubmittedAt { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    //Bu PDF hangi taleba ait
    public Guid? RequestId { get; set; }

    [ForeignKey(nameof(RequestId))]
    public ReportRequest? ReportRequest { get; set; }

    public Guid? SubmittedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}