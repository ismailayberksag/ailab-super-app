namespace ailab_super_app.DTOs.Report;
using ailab_super_app.Models.Enums;

public class ReportRequestDto
{
    public Guid Id { get; set; }

    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }

    public PeriodType? PeriodType { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public ReportRequestStatus Status { get; set; }

    // Hangi projelere atandı
    public List<TargetProjectDto> TargetProjects { get; set; } = new();
}

public class TargetProjectDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool PenaltyApplied { get; set; }
    public bool HasSubmitted { get; set; } // Bu proje rapor yüklemiş mi? (Helper alan)
}