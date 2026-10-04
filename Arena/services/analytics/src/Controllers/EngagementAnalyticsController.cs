using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AnalyticsService.DTOs;
using AnalyticsService.Repositories;

namespace AnalyticsService.Controllers;

/// <summary>
/// Exposes viewer engagement metrics and read-model aggregates to tournament organizers (SCRUM-125).
/// </summary>
[ApiController]
[Route("api/analytics/engagement")]
[Authorize(Roles = "Organizer")]
public class EngagementAnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<EngagementAnalyticsController> _logger;

    public EngagementAnalyticsController(
        IAnalyticsRepository analyticsRepository,
        ILogger<EngagementAnalyticsController> logger)
    {
        _analyticsRepository = analyticsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves engagement metrics across all broadcast streams, ordered by watch time and coins earned.
    /// Accessible exclusively to authenticated users with the Organizer role.
    /// </summary>
    /// <returns>A collection of stream engagement metrics.</returns>
    /// <response code="200">Engagement summaries returned successfully.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    /// <response code="403">Authenticated caller does not possess the required Organizer role.</response>
    [HttpGet("streams")]
    [ProducesResponseType(typeof(IEnumerable<StreamEngagementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStreamEngagement()
    {
        _logger.LogInformation("Retrieving stream engagement summaries for organizer dashboard");

        var summaries = await _analyticsRepository.GetAllEngagementSummariesAsync();

        var response = summaries.Select(s => new StreamEngagementResponse
        {
            StreamId = s.StreamId,
            TotalWatchSeconds = s.TotalWatchSeconds,
            TotalCoinsEarned = s.TotalCoinsEarned,
            TotalWatchTicks = s.TotalWatchTicks,
            UniqueViewers = s.UniqueViewers,
            LastEventAt = s.LastEventAt
        }).ToList();

        return Ok(response);
    }
}
