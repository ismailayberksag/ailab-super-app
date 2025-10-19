using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace ailab_super_app.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = default!;
    }

    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
