using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email veya kullanıcı adı gereklidir")]
        public string EmailOrUsername { get; set; } = default!;

        [Required(ErrorMessage = "Şifre gereklidir")]
        public string Password { get; set; } = default!;
    }
}
