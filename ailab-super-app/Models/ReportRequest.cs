using ailab_super_app.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
namespace ailab_super_app.Models;

public class ReportRequest
{
    public Guid Id { get; set; }

    // Talebi açan kullanıcı (Admin)
    public Guid CreatedBy { get; set; }
    [ForeignKey(nameof(CreatedBy))]
    public User CreatedByUser { get; set; } = default!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public PeriodType? PeriodType { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    // Talebin genel durumu (Örn: Aktif, Kapanmış)
    public ReportRequestStatus Status { get; set; } = ReportRequestStatus.Pending;

    // Bu talebe istinaden yüklenen tüm raporlar
    public ICollection<Report> SubmittedReports { get; set; } = new List<Report>();

    // Talebin atandığı projeler
    public ICollection<ReportRequestProject> TargetProjects { get; set; } = new List<ReportRequestProject>();

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

}