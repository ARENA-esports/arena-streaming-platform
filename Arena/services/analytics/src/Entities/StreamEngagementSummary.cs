namespace AnalyticsService.Entities;

/// <summary>
/// Read-model entity representing aggregated viewer engagement metrics per stream.
/// Populated by asynchronous consumption of Kafka events and stored in arena_analytics_db.
/// </summary>
public class StreamEngagementSummary
{
    /// <summary>
    /// Stream identifier (mapped from MatchId in the watch tick event contract).
    /// </summary>
    public int StreamId { get; set; }

    /// <summary>
    /// Cumulative watch time across all viewers in seconds.
    /// </summary>
    public long TotalWatchSeconds { get; set; }

    /// <summary>
    /// Total coins earned by viewers on this stream.
    /// </summary>
    public long TotalCoinsEarned { get; set; }

    /// <summary>
    /// Total number of successful watch tick events recorded for this stream.
    /// </summary>
    public int TotalWatchTicks { get; set; }

    /// <summary>
    /// Distinct viewers who have earned coins/watched this stream.
    /// </summary>
    public int UniqueViewers { get; set; }

    /// <summary>
    /// Timestamp (UTC) of the most recent watch tick event processed.
    /// </summary>
    public DateTime? LastEventAt { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
