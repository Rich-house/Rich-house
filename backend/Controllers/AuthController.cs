using Marketify.Contracts.Authenthication;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService auth) : ControllerBase
    {
        private readonly IAuthService _authService = auth;

        [HttpPost("Login")]
        public async Task<IActionResult>LoginAsync(LoginRequest request,CancellationToken cancellationToken = default)
        {
            var authResult = await _authService.GetTokenAsync(request.Email, request.Password);
            return authResult.IsSuccess ? Ok(authResult.Value) : BadRequest(authResult.Error);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestUser model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(model, cancellationToken);

            if (!string.IsNullOrEmpty(result))
            {
                return BadRequest(new { message = result });
            }

            return Ok(new { message = "Registration successful. Please check your email to confirm your account." });
        }
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var error = await _authService.ConfirmEmailAsync(model);

            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest(new { message = error });
            }

            return Ok(new { message = "Your email has been confirmed successfully! You can now log in." });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var message  = await _authService.ForgotPasswordAsync(model);
            return Ok(new { message = message });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(model);

                return Ok(new { message = result });

        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet(("me"))]
        public async Task<IActionResult> getUserinfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.GetUserInfoAsync(userId);
            if (result is null)
            {
                return Unauthorized();
            }

            return Ok(result);
        }
    }
}
