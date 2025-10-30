using ailab_super_app.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
namespace ailab_super_app.Models;

public class ReportRequest
{
    public Guid Id { get; set; }

    // Proje bazlı talep olmayabilir (bireysel talep)
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    //Talebi açan kullanıcı zorunlu (null olamaz)
    public Guid RequestedBy { get; set; }
    [ForeignKey(nameof(RequestedBy))]
    public User RequestedByUser { get; set; } = default!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public PeriodType? PeriodType { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    //Talebin durumu
    public ReportRequestStatus Status { get; set; } = ReportRequestStatus.Pending;

    //Bu talebe yüklenen PDF'ler birden fazla olabilir
    public ICollection<Report> SubmittedReports { get; set; } = new List<Report>();

    //Bireysel taleplerde hedef kullanıcılar (M2M)
    public ICollection<ReportRequestUser> TargetUsers { get; set; } = new List<ReportRequestUser>();

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

}