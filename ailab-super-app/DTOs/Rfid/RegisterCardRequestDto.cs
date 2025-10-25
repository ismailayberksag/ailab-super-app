using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Rfid;

public class RegisterCardRequestDto
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CardUid { get; set; } = string.Empty;
}
