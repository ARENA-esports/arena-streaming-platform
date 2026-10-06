using AnalyticsService.Entities;

namespace AnalyticsService.Repositories;

/// <summary>
/// Data access abstraction for the Analytics read model in arena_analytics_db.
/// Implementations must use raw ADO.NET with parameterized queries.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Atomically records a coin-earned / watch-tick event into the read-model.
    /// Handles duplicate event deduplication using an internal events ledger.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    /// <param name="userId">The viewer's user ID.</param>
    /// <param name="amount">The coin amount awarded.</param>
    /// <param name="timestampUtc">The UTC timestamp when the award was committed.</param>
    /// <param name="watchDurationSeconds">The watch duration in seconds attributed to this tick.</param>
    /// <returns><c>true</c> if event was recorded and aggregates updated; <c>false</c> if duplicate.</returns>
    Task<bool> RecordCoinEarnedAsync(
        int streamId,
        int userId,
        int amount,
        DateTime timestampUtc,
        int watchDurationSeconds);

    /// <summary>
    /// Retrieves engagement metrics for a specific stream.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    Task<StreamEngagementSummary?> GetEngagementSummaryByStreamIdAsync(int streamId);

    /// <summary>
    /// Retrieves engagement metrics across all streams.
    /// </summary>
    Task<IReadOnlyList<StreamEngagementSummary>> GetAllEngagementSummariesAsync();

    /// <summary>
    /// Retrieves aggregated battle and attack metrics for a specific stream across all participating teams.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    Task<IReadOnlyList<StreamTeamBattleSummary>> GetBattleSummariesByStreamIdAsync(int streamId);

    /// <summary>
    /// Retrieves aggregated battle and attack metrics across all streams and teams.
    /// </summary>
    Task<IReadOnlyList<StreamTeamBattleSummary>> GetAllBattleSummariesAsync();

    /// <summary>
    /// Retrieves round outcome history for a specific stream ordered by round number ascending.
    /// </summary>
    /// <param name="streamId">The stream/match identifier.</param>
    Task<IReadOnlyList<StreamRoundOutcome>> GetRoundOutcomesByStreamIdAsync(int streamId);

    /// <summary>
    /// Retrieves round outcome history across all streams.
    /// </summary>
    Task<IReadOnlyList<StreamRoundOutcome>> GetAllRoundOutcomesAsync();

    /// <summary>
    /// Upserts aggregated battle metrics for a stream and team into the read model.
    /// </summary>
    /// <param name="summary">The battle summary record to upsert.</param>
    Task UpsertBattleSummaryAsync(StreamTeamBattleSummary summary);

    /// <summary>
    /// Records a completed round outcome into the read model.
    /// </summary>
    /// <param name="outcome">The round outcome record to insert.</param>
    Task RecordRoundOutcomeAsync(StreamRoundOutcome outcome);
}
