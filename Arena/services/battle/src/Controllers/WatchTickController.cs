using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BattleEconomyService.DTOs;
using BattleEconomyService.Services;

namespace BattleEconomyService.Controllers;

/// <summary>
/// Handles viewer economy interactions including passive coin-earning watch ticks.
/// </summary>
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
    /// Records a stream watch tick for the authenticated viewer and awards coins if the minimum interval has elapsed.
    /// </summary>
    /// <param name="request">Optional watch tick payload containing stream context.</param>
    /// <returns>Updated coin balance on success, or HTTP 429 if the anti-farm check fails.</returns>
    /// <response code="200">Watch tick recorded successfully; coins awarded.</response>
    /// <response code="400">Invalid request payload, invalid stream ID, or stream is not currently live (SCRUM-118).</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    /// <response code="429">Anti-farm or coin-cap check triggered; minimum interval not elapsed, or 5-minute window ceiling reached.</response>
    [HttpPost("watch-tick")]
    [Authorize]
    [ProducesResponseType(typeof(WatchTickResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WatchTickResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(WatchTickResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AwardWatchTick([FromBody] WatchTickRequest? request)
    {
        // 1. Authenticated user ID resolution from trusted JWT claims only (sub or NameIdentifier)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            _logger.LogWarning("Watch-tick rejected: invalid or missing user ID claim.");
            return Unauthorized(new { message = "Invalid or missing user identity claim in token." });
        }

        // 2. Validate request parameters
        if (request?.StreamId.HasValue == true && request.StreamId.Value <= 0)
        {
            return BadRequest(new { message = "Invalid stream ID. Must be a positive integer." });
        }

        // 3. Process watch tick through the business layer
        var result = await _watchTickService.ProcessWatchTickAsync(userId, request?.StreamId);

        // 4. Handle coin-cap violation (SCRUM-115 AC1) — 5-minute rolling window ceiling reached
        if (result.Status == WatchTickStatus.CapExceeded)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new WatchTickResponse
            {
                Success = false,
                CoinsAwarded = 0,
                CurrentBalance = result.CurrentBalance,
                LastTickAt = result.LastTickAt,
                Message = result.Message
            });
        }

        // 5. Handle rate-limited anti-farm violation (AC2)
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

        // 6. Handle stream not live (SCRUM-118 AC2)
        if (result.Status == WatchTickStatus.StreamNotLive)
        {
            return BadRequest(new WatchTickResponse
            {
                Success = false,
                CoinsAwarded = 0,
                CurrentBalance = result.CurrentBalance,
                LastTickAt = result.LastTickAt,
                Message = result.Message
            });
        }

        // 7. Handle successful award
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

        return BadRequest(new WatchTickResponse
        {
            Success = false,
            CoinsAwarded = 0,
            CurrentBalance = result.CurrentBalance,
            LastTickAt = result.LastTickAt,
            Message = result.Message
        });
    }
}
