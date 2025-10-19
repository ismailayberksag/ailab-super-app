using System.ComponentModel.DataAnnotations;

namespace ailab_super_app.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token gereklidir")]
    public string RefreshToken { get; set; } = default!;
}