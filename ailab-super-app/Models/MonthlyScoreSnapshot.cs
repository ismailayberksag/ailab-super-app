using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ailab_super_app.Models;

[Table("monthly_score_snapshots")]
public class MonthlyScoreSnapshot
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string UserName { get; set; } = default!;

    [Required]
    public decimal TotalScore { get; set; }

    [Required]
    [MaxLength(20)]
    public string Period { get; set; } = default!; // Örn: "2025-12"

    [Required]
    public DateTime SnapshotDate { get; set; } // Resetleme anı (UTC+3)
}
