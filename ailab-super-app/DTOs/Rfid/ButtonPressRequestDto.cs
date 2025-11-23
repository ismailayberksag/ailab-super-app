using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Rfid;

public class ButtonPressRequestDto
{
    [Required]
    [MaxLength(100)]
    public string ButtonUid { get; set; } = string.Empty;
}


