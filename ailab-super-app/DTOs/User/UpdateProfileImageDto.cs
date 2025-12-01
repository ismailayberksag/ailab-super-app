using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.User
{
    public class UpdateProfileImageDto
    {
        [Required(ErrorMessage = "Profil resmi URL'i zorunludur.")]
        [Url(ErrorMessage = "Geçerli bir URL formatı giriniz.")]
        public string ProfileImageUrl { get; set; } = default!;
    }
}
