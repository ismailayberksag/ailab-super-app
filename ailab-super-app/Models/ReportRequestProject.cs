using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

public class ReportRequestProject
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ReportRequestId { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    public bool PenaltyApplied { get; set; } = false;
    public DateTime? PenaltyAppliedAt { get; set; }

    [ForeignKey(nameof(ReportRequestId))]
    public ReportRequest ReportRequest { get; set; } = default!;

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = default!;
}