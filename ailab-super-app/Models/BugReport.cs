using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ailab_super_app.Models.Enums;

namespace ailab_super_app.Models;

[Table("bug_reports")]
public class BugReport
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public PlatformType Platform { get; set; }

    [Required]
    [MaxLength(250)]
    public string PageInfo { get; set; } = default!;

    [Required]
    public BugType BugType { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = default!;

    [Required]
    public Guid ReportedByUserId { get; set; }
    public virtual User ReportedByUser { get; set; } = default!;

    public DateTime ReportedAt { get; set; }

    public bool IsResolved { get; set; } = false;
}
