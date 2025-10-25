using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Rfid;

public class CardScanRequestDto
{
    [Required]
    [MaxLength(50)]
    public string CardUid { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ReaderUid { get; set; } = string.Empty;
}
