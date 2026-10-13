using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/auth/twitch")]
public class TwitchAuthController : ControllerBase
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public TwitchAuthController(
        IExternalAuthService externalAuthService,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _externalAuthService = externalAuthService;
        _configuration = configuration;
        _env = env;
    }

    /// <summary>
    /// Initiates Twitch OAuth authorization challenge.
    /// </summary>
    [HttpGet]
    public IActionResult ChallengeTwitch()
    {
        var redirectUrl = Url.Action(nameof(Complete), "TwitchAuth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Twitch");
    }

    /// <summary>
    /// Invoked after Twitch OAuth middleware completes authentication and signs in to "External" scheme.
    /// </summary>
    [HttpGet("complete")]
    public async Task<IActionResult> Complete()
    {
        var frontendBase = _configuration["ExternalAuth:FrontendBaseUrl"] ?? "http://localhost:5173";
        var authResult = await HttpContext.AuthenticateAsync("External");
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            return Redirect($"{frontendBase}/auth/callback?error=twitch_authentication_failed");
        }

        try
        {
            var code = await _externalAuthService.ProcessTwitchLoginAsync(authResult.Principal);
            await HttpContext.SignOutAsync("External");

            return Redirect($"{frontendBase}/auth/callback?code={code}");
        }
        catch (Exception ex)
        {
            await HttpContext.SignOutAsync("External");
            return Redirect($"{frontendBase}/auth/callback?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// Exchanges single-use authorization code for an Arena JWT.
    /// </summary>
    [HttpPost("exchange")]
    public async Task<IActionResult> Exchange([FromBody] TwitchExchangeRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var response = await _externalAuthService.ExchangeCodeAsync(request.Code);

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
            return Unauthorized(new { message = ex.Message });
        }
    }
}