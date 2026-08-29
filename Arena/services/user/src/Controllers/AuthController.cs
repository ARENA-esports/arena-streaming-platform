using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
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
    /// Retrieves current authenticated user profile.
    /// </summary>
    /// <returns>The authenticated user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult GetMe()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value;
        var username = User.Identity?.Name
            ?? User.FindFirst("unique_name")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        return Ok(new
        {
            userId = int.TryParse(sub, out var id) ? id : 0,
            username = username ?? string.Empty,
            email = email ?? string.Empty,
            role = role ?? string.Empty
        });
    }
}
