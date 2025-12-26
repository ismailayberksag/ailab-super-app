using ailab_super_app.Data;
using ailab_super_app.DTOs.Auth;
using ailab_super_app.Models;
using ailab_super_app.Configuration;
using ailab_super_app.Helpers; // GetTurkeyTime için eklendi
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ailab_super_app.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            UserManager<User> userManager,
            AppDbContext context,
            IOptions<JwtSettings> jwtSettings)
        {
            _userManager = userManager;
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress)
        {
            var now = DateTimeHelper.GetTurkeyTime();

            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                throw new Exception("Bu email adresi zaten kullanılıyor!");
            }
            if (await _userManager.FindByNameAsync(request.FullName) != null)
            {
                throw new Exception("Bu kullanıcı adı zaten kullanılıyor!");
            }

            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                SchoolNumber = request.SchoolNumber,
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                ProfileImageUrl = "https://firebasestorage.googleapis.com/v0/b/ailab-super-app.firebasestorage.app/o/default.webp?alt=media"
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı oluşturulamadı: {errors}");
            }

            var roleAssignResult = await _userManager.AddToRoleAsync(user, "Member");
            if (!roleAssignResult.Succeeded)
            {
                var errors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                throw new Exception($"Varsayılan rol atanamadı (Member): {errors}");
            }

            return await GenerateLoginResponse(user, ipAddress);
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(request.EmailOrUsername)
                ?? await _userManager.FindByNameAsync(request.EmailOrUsername);

            if (user == null)
                throw new Exception("Kullanıcı adı veya şifre hatalı");

            if (user.IsDeleted)
                throw new Exception("Kullanıcı bulunamadı");

            if (user.Status != UserStatus.Active)
                throw new Exception("Kullanıcı aktif değil");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
                throw new Exception("Kullanıcı adı veya şifre hatalı");

            return await GenerateLoginResponse(user, ipAddress);
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (existingToken == null)
                throw new Exception("Geçersiz refresh token");

            if (existingToken.IsRevoked)
                throw new Exception("Refresh token iptal edilmiş");

            if (existingToken.ExpiresAt < now)
                throw new Exception("Refresh token süresi dolmuş");

            if (existingToken.User.IsDeleted)
                throw new Exception("Bu hesap silinmiş");

            existingToken.IsRevoked = true;
            existingToken.RevokedAt = now;
            existingToken.RevokedByIp = ipAddress;

            var newRefreshToken = GenerateRefreshToken(ipAddress);
            newRefreshToken.ReplacedByToken = existingToken.Token;
            existingToken.ReplacedByToken = newRefreshToken.Token;

            await _context.SaveChangesAsync();

            return await GenerateLoginResponse(existingToken.User, ipAddress);
        }

        public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
                throw new Exception("Geçersiz refresh token");

            if (token.IsRevoked)
                throw new Exception("Refresh token zaten iptal edilmiş");

            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || token.IsRevoked || token.ExpiresAt < now)
                return false;

            return true;
        }

        #region Private Methods
        private async Task<LoginResponseDto> GenerateLoginResponse(User user, string ipAddress)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var accessToken = await GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(ipAddress);

            refreshToken.UserId = user.Id;
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(user);

            return new LoginResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    FullName = user.FullName,
                    ProfileImageUrl = user.ProfileImageUrl,
                    Roles = roles.ToList()
                }
            };
        }

        private async Task<string> GenerateAccessToken(User user)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.FullName ?? ""),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var now = DateTimeHelper.GetTurkeyTime();
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = now,
                CreatedByIp = ipAddress
            };
        }

        #endregion
    }
}