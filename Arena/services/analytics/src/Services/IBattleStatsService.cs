using AnalyticsService.DTOs;

namespace AnalyticsService.Services;

/// <summary>
/// Application service coordinating battle statistics and round outcomes for organizer analytics (SCRUM-126).
/// </summary>
public interface IBattleStatsService
{
    /// <summary>
    /// Retrieves aggregated battle metrics across all streams and teams.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<StreamTeamBattleResponse>> GetAllBattleSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated battle metrics for a specific stream across all participating teams.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<StreamTeamBattleResponse>> GetBattleSummariesByStreamIdAsync(int streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves sequential round outcomes for a specific stream.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<StreamRoundOutcomeResponse>> GetRoundOutcomesByStreamIdAsync(int streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the combined battle dashboard data (team summaries and round outcomes) for a specific stream.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<StreamBattleDashboardResponse> GetStreamBattleDashboardAsync(int streamId, CancellationToken cancellationToken = default);
}
