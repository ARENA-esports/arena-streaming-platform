using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BattleEconomyService.DTOs;
using BattleEconomyService.Services;

namespace BattleEconomyService.Controllers;

[ApiController]
[Route("api/economy")]
public class WatchTickController : ControllerBase
{
    private readonly IWatchTickService _watchTickService;
    private readonly ILogger<WatchTickController> _logger;

    public WatchTickController(IWatchTickService watchTickService, ILogger<WatchTickController> logger)
    {
        _watchTickService = watchTickService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a stream watch tick for the authenticated viewer.
    /// Awards coins if >= 55 seconds elapsed since last successful award.
    /// Returns 429 Too Many Requests if interval is not met (anti-farm check).
    /// </summary>
    [HttpPost("watch-tick")]
    [Authorize]
    [ProducesResponseType(typeof(WatchTickResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WatchTickResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AwardWatchTick([FromBody] WatchTickRequest? request)
    {
        // Extract authenticated user ID from JWT claims (NameIdentifier or sub)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing user identity claim in token." });
        }

        var result = await _watchTickService.ProcessWatchTickAsync(userId, request?.StreamId);

        if (result.Status == WatchTickStatus.RateLimited)
        {
            if (result.RemainingSeconds.HasValue)
            {
                Response.Headers.Append("Retry-After", result.RemainingSeconds.Value.ToString());
            }

            return StatusCode(StatusCodes.Status429TooManyRequests, new WatchTickResponse
            {
                Success = false,
                CoinsAwarded = 0,
                CurrentBalance = result.CurrentBalance,
                LastTickAt = result.LastTickAt,
                RemainingSeconds = result.RemainingSeconds,
                Message = result.Message
            });
        }

        if (result.Status == WatchTickStatus.Success)
        {
            return Ok(new WatchTickResponse
            {
                Success = true,
                CoinsAwarded = result.CoinsAwarded,
                CurrentBalance = result.CurrentBalance,
                LastTickAt = result.LastTickAt,
                Message = result.Message
            });
        }

        return BadRequest(new { message = result.Message });
    }
}
