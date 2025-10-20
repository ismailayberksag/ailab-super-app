using ailab_super_app.DTOs.Auth;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ailab_super_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
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
