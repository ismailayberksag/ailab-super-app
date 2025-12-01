using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.User;

public class UpdateAvatarUrlDto
{
    [Required(ErrorMessage = "Avatar URL'i gereklidir.")]
    [Url(ErrorMessage = "Geçerli bir URL olmalıdır.")]
    [MaxLength(1000, ErrorMessage = "Avatar URL'i 1000 karakterden uzun olamaz.")]
    public string AvatarUrl { get; set; } = string.Empty;
}
