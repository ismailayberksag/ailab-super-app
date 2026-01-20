using ailab_super_app.Data;
using ailab_super_app.DTOs.Auth;
using ailab_super_app.Models;
using ailab_super_app.Models.Enums;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IFirebaseAuthService firebaseAuthService,
            UserManager<User> userManager,
            AppDbContext context,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _firebaseAuthService = firebaseAuthService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        ///<summary>
        ///Register a new user
        ///</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
        {
            try
            {
                var ipAdress = GetIpAddress();
                var result = await _authService.RegisterAsync(registerRequest, ipAdress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Register hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        ///<summary>
        ///Login user
        ///</summary>
        ///
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                var ipAdress = GetIpAddress();
                var result = await _authService.LoginAsync(loginRequest, ipAdress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Login with Firebase Token
        /// </summary>
        [HttpPost("login-firebase")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithFirebase([FromBody] FirebaseLoginRequest request)
        {
            try
            {
                var user = await _firebaseAuthService.AuthenticateWithFirebaseAsync(request.IdToken);

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    userId = user.Id,
                    email = user.Email,
                    userName = user.UserName,
                    roles = roles,
                    authProvider = user.AuthProvider.ToString()
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Firebase login failed: {Message}", ex.Message);
                return Unauthorized(new { message = "Invalid Firebase token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Firebase login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create Firebase user for existing Legacy user (Admin only)
        /// </summary>
        [HttpPost("admin/create-firebase-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFirebaseUserForExisting([FromBody] CreateFirebaseUserRequest request)
        {
            try
            {
                // 1. Kullanıcının sistemde olduğunu kontrol et
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return NotFound(new { message = "User not found in system" });
                }

                if (user.AuthProvider == AuthProvider.Firebase)
                {
                    return BadRequest(new { message = "User already migrated to Firebase" });
                }

                // 2. Firebase'de kullanıcı oluştur ve reset linki al
                var resetLink = await _firebaseAuthService
                    .CreateFirebaseUserForExistingUserAsync(request.Email, request.TemporaryPassword);

                return Ok(new
                {
                    message = "Firebase user created successfully",
                    passwordResetLink = resetLink,
                    instructions = "Send this link to user. When they reset password and login, migration will complete automatically."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Firebase user");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get Migration Status (Admin only)
        /// </summary>
        [HttpGet("admin/migration-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMigrationStatus()
        {
            var totalUsers = await _context.Users.CountAsync();
            var migratedUsers = await _context.Users.CountAsync(u => u.AuthProvider == AuthProvider.Firebase);
            var legacyUsers = await _context.Users.CountAsync(u => u.AuthProvider == AuthProvider.Legacy);

            var legacyUserList = await _context.Users
                .Where(u => u.AuthProvider == AuthProvider.Legacy)
                .Select(u => new { u.Email, u.UserName })
                .ToListAsync();

            return Ok(new
            {
                total = totalUsers,
                migrated = migratedUsers,
                legacy = legacyUsers,
                legacyUserEmails = legacyUserList
            });
        }

        ///<summary>
        ///Refresh Access Token
        /// </summary> 
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Refresh token hatası: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
        }

        ///<summary>
        ///Revoke Refresh Token (logout)
        ///</summary>  
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var ipAddress = GetIpAddress();
                await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);
                return Ok(new { message = "Çıkış başarılı." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Logout hatası: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        ///<summary>
        ///User Information(for testing purposes, authorize required)
        ///</summary>

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new
            {
                id = userId,
                userName = userName,
                email = email
            });
        }
        #region Private Methods

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        #endregion
    }
}