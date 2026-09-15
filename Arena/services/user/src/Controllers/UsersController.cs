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
[Authorize]
[EnableRateLimiting("GeneralApiLimiter")]   //hard: Bound user profile updates by general rate limits to prevent script-driven database spam
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    //hard: Injected ILogger and IWebHostEnvironment to resolve compilation errors
    private readonly ILogger<UsersController> _logger;
    private readonly IWebHostEnvironment _env;

    public UsersController(IUserService userService, ILogger<UsersController> logger, IWebHostEnvironment env)
    {
        _userService = userService;
        _logger = logger;
        _env = env;
    }

    //hard: BOLA / IDOR Defense: User identity is extracted exclusively from cryptographically signed JWT claims
    private int? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(sub, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Retrieves current authenticated user profile.
    /// </summary>
    /// <returns>The authenticated user's profile information</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Invalid user identifier claim." });
        }

        var profile = await _userService.GetProfileAsync(userId.Value);
        if (profile == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Updates current authenticated user profile (username, email, avatar URL).
    /// </summary>
    /// <param name="request">Fields to update</param>
    /// <returns>The updated user profile</returns>
    [HttpPut("me")]
    [RequestSizeLimit(32768)]   //hard: Capped profile mutation payload size to 32KB to prevent buffer flooding
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Invalid user identifier claim." });
        }

        //hard: Stored XSS defense: ensure avatar URL uses standard HTTP/HTTPS schemes if provided
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
            !request.AvatarUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !request.AvatarUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Avatar URL must use http or https scheme." });
        }
        
        try
        {
            var updatedProfile = await _userService.UpdateProfileAsync(userId.Value, request);
            return Ok(updatedProfile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Exclude ex.Message from 500 response; log details internally
            _logger.LogError(ex, "An unhandled error occurred while updating profile for user {UserId}", userId.Value);
            return StatusCode(500, new { message = "An error occurred while updating the profile.", details = ex.Message });
        }
    }

    /// <summary>
    /// Changes the password for the current authenticated user.
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <returns>Confirmation message of password change</returns>
    [HttpPut("me/password")]
    [RequestSizeLimit(2048)]    //hard: Capped password change payload size to 2KB
    [ProducesResponseType(typeof(ChangePasswordResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Invalid user identifier claim." });
        }

        try
        {
            var response = await _userService.ChangePasswordAsync(userId.Value, request);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Avoid leaking internal database or hashing exceptions
            _logger.LogError(ex, "An unhandled error occurred while changing password for user {UserId}", userId.Value);
            return StatusCode(500, new { message = "An error occurred while changing the password.", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes the current authenticated user account completely.
    /// </summary>
    /// <returns>Confirmation of deletion</returns>
    [HttpDelete("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Invalid user identifier claim." });
        }

        try
        {
            await _userService.DeleteAccountAsync(userId.Value);
            //hard: Ensure authentication cookie is destroyed upon account deletion
            Response.Cookies.Delete("arena_access_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new { message = "Account successfully deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            //hard: CWE-209 fix- Suppress raw database exception details
            _logger.LogError(ex, "An unhandled error occurred while deleting account for user {UserId}", userId.Value);
            return StatusCode(500, new { message = "An error occurred while deleting the account.", details = ex.Message });
        }
    }
}
