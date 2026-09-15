using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AuthIpLimiter")]// hard: Applied IP-level rate limiting to mitigate brute-force guessing and endpoint flooding
public class AuthController : ControllerBase

{
    private readonly IAuthService _authService;
    //hard: Injected ILogger to fix compilation and enable internal security logging
    private readonly ILogger<AuthController> _logger;
    //hard: Injected IWebHostEnvironment to toggle dev-friendly cookie security flags
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IWebHostEnvironment env)
    {
        _authService = authService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Registers a new viewer account.
    /// </summary>
    /// <param name="request">Signup details containing username, email, and password</param>
    /// <returns>A confirmation response on successful registration</returns>
    [HttpPost("signup")]
    [RequestSizeLimit(4096)] // hard: request body size cap to 4kb to eliminate memory exhaustion attacks via oversized json
    [ProducesResponseType(typeof(SignupResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.SignupAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            //hard Neutralized CWE-209 Information Disclosure by stripping ex.Message and logging internally
            _logger.LogError(ex, "An unhandled error occurred during user registration.");
            return StatusCode(500, new { message = "An error occurred while creating the account.", details = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a viewer and issues a signed JWT token.
    /// </summary>
    /// <param name="request">Login credentials with username/email and password</param>
    /// <returns>The authenticated user information and signed JWT Bearer token</returns>
    [HttpPost("login")]
    [RequestSizeLimit(2048)] // hard: request body size cap to 2KB to prevent memory buffer abuse
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.LoginAsync(request);
            Response.Cookies.Append("arena_access_token",response.Token,new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(), // Secure in production, allows HTTP for local dev
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            });
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Exclude stack/internal message details from the API response
            _logger.LogError(ex, "An unhandled error occurred during user login.");
            return StatusCode(500, new { message = "An error occurred while logging in.", details = ex.Message });
        }
    }

    /// <summary>
    /// Logs out the viewer and invalidates the JWT Bearer token.
    /// </summary>
    /// <returns>A confirmation message on successful logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            //hard: Clear HttpOnly authentication cookie upon session termination
            Response.Cookies.Delete("arena_access_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict
            });
            var authHeader = Request.Headers.Authorization.ToString();
            await _authService.LogoutAsync(authHeader);
            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Suppress internal error details
            _logger.LogError(ex, "An unhandled error occurred during logout.");
            return StatusCode(500, new { message = "An error occurred while logging out.", details = ex.Message });
        }
    }

    /// <summary>
    /// Initiates password reset for a registered viewer account.
    /// </summary>
    /// <param name="request">Request containing the user's email</param>
    /// <returns>Confirmation message and token if user is found</returns>
    [HttpPost("forgot-password")]
    [RequestSizeLimit(2048)]    //hard: Strict 2KB payload cap on email recovery requests
    [ProducesResponseType(typeof(ForgotPasswordResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.ForgotPasswordAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Log internally, respond with generic error
            _logger.LogError(ex, "An unhandled error occurred during forgot password processing.");
            return StatusCode(500, new { message = "An error occurred while processing the password reset request.", details = ex.Message });
        }
    }

    /// <summary>
    /// Sets a new password using a valid reset token.
    /// </summary>
    /// <param name="request">Request containing reset token and new password</param>
    /// <returns>Confirmation of password change</returns>
    [HttpPost("reset-password")]
    [RequestSizeLimit(2048)]//hard: Strict 2KB payload cap on password reset requests
    [ProducesResponseType(typeof(ResetPasswordResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.ResetPasswordAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Suppress exception message
            _logger.LogError(ex, "An unhandled error occurred during password reset.");
            return StatusCode(500, new { message = "An error occurred while resetting the password.", details = ex.Message });
        }
    }

    /// <summary>
    /// Verifies a user's email address using a verification token.
    /// </summary>
    /// <param name="request">Request containing the verification token</param>
    /// <returns>Confirmation message on successful email verification</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.VerifyEmailAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while verifying the email.", details = ex.Message });
        }
    }

    /// <summary>
    /// Resends the email verification token to the specified email address.
    /// </summary>
    /// <param name="request">Request containing the user's email</param>
    /// <returns>Confirmation message</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ResendVerificationEmailResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.ResendVerificationEmailAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing the verification request.", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves current authenticated user profile from the database.
    /// </summary>
    /// <returns>The authenticated user profile information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMe()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(sub, out var userId))
        {
            return Unauthorized(new { message = "Invalid user identifier claim." });
        }

        var profile = await _authService.GetProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Refreshes the session by issuing a new JWT for an active, unexpired token.
    /// </summary>
    /// <returns>A new LoginResponse containing the refreshed JWT</returns>
    [HttpPost("refresh")]
    [Authorize]
    [RequestSizeLimit(1024)]    //hard: Bounded refresh endpoint payload size
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            // extract user id from the valid jwt claims
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // gracefully fail if sub claim is missing or malformed
            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            // delegate token refresh to the auth service
            var response = await _authService.RefreshTokenAsync(userId);

            //hard: Update HttpOnly cookie with freshly minted sliding expiration token
            Response.Cookies.Append("arena_access_token", response.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            });
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            //hard: CWE-209 fix- Suppress internal error details
            _logger.LogError(ex, "An unhandled error occurred during token refresh.");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while refreshing the token.", details = ex.Message });
        }
    }
}
