using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Email gereklidir!")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz!")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Kullanıcı adı gereklidir!")]
        [MinLength(3, ErrorMessage = "Kullanıcı adı en az 3 karakter olmalıdır!")]
        [MaxLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir!")]
        public string UserName { get; set; } = default!;

        // Yeni: SchoolNumber (opsiyonel tutuluyor; isterseniz Required yapabilirsiniz)
        [MaxLength(50)]
        public string? SchoolNumber { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir!")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır!")]
        public string Password { get; set; } = default!;

        [Required(ErrorMessage = "Ad Soyad gereklidir!")]
        [MaxLength(200, ErrorMessage = "Ad Soyad en fazla 200 karakter olabilir!")]
        public string FullName { get; set; } = default!;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz!")]
        public string? PhoneNumber { get; set; }
    }
}
