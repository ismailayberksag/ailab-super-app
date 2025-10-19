using ailab_super_app.DTOs.Auth;

namespace ailab_super_app.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
        Task<LoginResponseDto> RefreshTokenAsync(string token, string ipAddress);
        Task RevokeTokenAsync (string refreshToken, string ipAddress);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    }
}
