using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

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
            return StatusCode(500, new { message = "An error occurred while updating the profile.", details = ex.Message });
        }
    }

    /// <summary>
    /// Changes the password for the current authenticated user.
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <returns>Confirmation message of password change</returns>
    [HttpPut("me/password")]
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
            return StatusCode(500, new { message = "An error occurred while changing the password.", details = ex.Message });
        }
    }
}
