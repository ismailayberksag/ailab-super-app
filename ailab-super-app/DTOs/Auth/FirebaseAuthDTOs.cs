using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Auth
{
    public class FirebaseLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = default!;

        // Opsiyonel Kayıt Bilgileri (Sadece ilk kayıtta gönderilir)
        public string? FullName { get; set; }
        public string? SchoolNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserName { get; set; } // Eğer email yerine özel bir username istenirse
    }

    public class CreateFirebaseUserRequest
    {
        public string Email { get; set; } = default!;
        public string TemporaryPassword { get; set; } = default!; 
    }
}