using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.User;

public class UpdateUserDto
{
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}
