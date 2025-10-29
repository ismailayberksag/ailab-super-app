using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

public class ReportRequestUser
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ReportRequestId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(ReportRequestId))]
    public ReportRequest ReportRequest { get; set; } = default!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = default!;
}