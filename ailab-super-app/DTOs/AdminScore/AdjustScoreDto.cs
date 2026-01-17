using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.AdminScore;

public class AdjustScoreDto
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = default!;
}
