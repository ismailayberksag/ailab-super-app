namespace ailab_super_app.DTOs.Report;
using ailab_super_app.Models.Enums;

public class ReportDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    public PeriodType? PeriodType { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }

    public Guid? RequestId { get; set; }

    public Guid? SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }

    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}



