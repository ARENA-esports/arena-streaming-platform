using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new viewer account.
    /// </summary>
    /// <param name="request">Signup details containing username, email, and password</param>
    /// <returns>A confirmation response on successful registration</returns>
    [HttpPost("signup")]
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
            return StatusCode(500, new { message = "An error occurred while creating the account.", details = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a viewer and issues a signed JWT token.
    /// </summary>
    /// <param name="request">Login credentials with username/email and password</param>
    /// <returns>The authenticated user information and signed JWT Bearer token</returns>
    [HttpPost("login")]
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
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
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
            var authHeader = Request.Headers.Authorization.ToString();
            await _authService.LogoutAsync(authHeader);
            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while logging out.", details = ex.Message });
        }
    }

    /// <summary>
    /// Initiates password reset for a registered viewer account.
    /// </summary>
    /// <param name="request">Request containing the user's email</param>
    /// <returns>Confirmation message and token if user is found</returns>
    [HttpPost("forgot-password")]
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
            return StatusCode(500, new { message = "An error occurred while processing the password reset request.", details = ex.Message });
        }
    }

    /// <summary>
    /// Sets a new password using a valid reset token.
    /// </summary>
    /// <param name="request">Request containing reset token and new password</param>
    /// <returns>Confirmation of password change</returns>
    [HttpPost("reset-password")]
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
}
