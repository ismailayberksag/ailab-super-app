namespace ailab_super_app.DTOs.Report;
using ailab_super_app.Models.Enums;

public class ReportRequestDto
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }

    public Guid RequestedBy { get; set; }
    public string RequestedByName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }

    public PeriodType? PeriodType { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<TargetUserDto> TargetUsers { get; set; } = new();
    public List<ReportDto> SubmittedReports { get; set; } = new();
}

public class TargetUserDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
}



