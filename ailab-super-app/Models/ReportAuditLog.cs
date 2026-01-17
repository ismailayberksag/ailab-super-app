using ailab_super_app.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

public class ReportAuditLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ReportId { get; set; }

    [Required]
    public string Action { get; set; } = default!; // Uploaded, Approved, Rejected, etc.

    [Required]
    public Guid PerformedByUserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? Comment { get; set; }

    [ForeignKey(nameof(ReportId))]
    public Report Report { get; set; } = default!;

    [ForeignKey(nameof(PerformedByUserId))]
    public User PerformedByUser { get; set; } = default!;
}