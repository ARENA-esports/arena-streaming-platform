using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AnalyticsService.DTOs;
using AnalyticsService.Services;

namespace AnalyticsService.Controllers;

/// <summary>
/// Exposes battle and attack statistics per stream and team to tournament organizers (SCRUM-126).
/// </summary>
[ApiController]
[Route("api/analytics/battle")]
[Authorize(Roles = "Organizer")]
public class BattleAnalyticsController : ControllerBase
{
    private readonly IBattleStatsService _battleStatsService;
    private readonly ILogger<BattleAnalyticsController> _logger;

    public BattleAnalyticsController(
        IBattleStatsService battleStatsService,
        ILogger<BattleAnalyticsController> logger)
    {
        _battleStatsService = battleStatsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves battle and attack metrics across all streams and teams.
    /// Accessible exclusively to authenticated users with the Organizer role.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of stream team battle metrics.</returns>
    /// <response code="200">Battle summaries returned successfully.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    /// <response code="403">Authenticated caller does not possess the required Organizer role.</response>
    [HttpGet("streams")]
    [ProducesResponseType(typeof(IEnumerable<StreamTeamBattleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllBattleSummaries(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving battle summaries across all streams for organizer dashboard");
        var summaries = await _battleStatsService.GetAllBattleSummariesAsync(cancellationToken);
        return Ok(summaries);
    }

    /// <summary>
    /// Retrieves complete battle dashboard metrics (team attack summaries and round outcomes) for a specific stream.
    /// Accessible exclusively to authenticated users with the Organizer role.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated battle dashboard data for the stream.</returns>
    /// <response code="200">Battle dashboard returned successfully.</response>
    /// <response code="400">Stream identifier is non-positive or invalid.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    /// <response code="403">Authenticated caller does not possess the required Organizer role.</response>
    [HttpGet("streams/{streamId:int}")]
    [ProducesResponseType(typeof(StreamBattleDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStreamBattleDashboard(int streamId, CancellationToken cancellationToken)
    {
        if (streamId <= 0)
        {
            _logger.LogWarning("Invalid stream identifier requested: {StreamId}", streamId);
            return BadRequest("Invalid stream identifier. StreamId must be a positive integer.");
        }

        _logger.LogInformation("Retrieving battle dashboard for stream {StreamId}", streamId);
        var dashboard = await _battleStatsService.GetStreamBattleDashboardAsync(streamId, cancellationToken);
        return Ok(dashboard);
    }

    /// <summary>
    /// Retrieves sequential round outcomes for a specific stream.
    /// Accessible exclusively to authenticated users with the Organizer role.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of round outcomes for the stream.</returns>
    /// <response code="200">Round outcomes returned successfully.</response>
    /// <response code="400">Stream identifier is non-positive or invalid.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    /// <response code="403">Authenticated caller does not possess the required Organizer role.</response>
    [HttpGet("streams/{streamId:int}/rounds")]
    [ProducesResponseType(typeof(IEnumerable<StreamRoundOutcomeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoundOutcomesByStreamId(int streamId, CancellationToken cancellationToken)
    {
        if (streamId <= 0)
        {
            _logger.LogWarning("Invalid stream identifier requested for rounds: {StreamId}", streamId);
            return BadRequest("Invalid stream identifier. StreamId must be a positive integer.");
        }

        _logger.LogInformation("Retrieving round outcomes for stream {StreamId}", streamId);
        var rounds = await _battleStatsService.GetRoundOutcomesByStreamIdAsync(streamId, cancellationToken);
        return Ok(rounds);
    }
}
