namespace AnalyticsService.DTOs;

/// <summary>
/// Data transfer object representing viewer engagement metrics for a broadcast stream.
/// Exposes read-model metrics to tournament organizers viewing the engagement dashboard.
/// </summary>
public class StreamEngagementResponse
{
    /// <summary>
    /// Unique identifier of the stream/match.
    /// </summary>
    public int StreamId { get; set; }

    /// <summary>
    /// Cumulative watch time in seconds across all viewers for this stream.
    /// </summary>
    public long TotalWatchSeconds { get; set; }

    /// <summary>
    /// Total coins earned by viewers during this stream broadcast.
    /// </summary>
    public long TotalCoinsEarned { get; set; }

    /// <summary>
    /// Total count of successful watch ticks recorded for this stream.
    /// </summary>
    public int TotalWatchTicks { get; set; }

    /// <summary>
    /// Count of distinct viewers who have earned coins while watching this stream.
    /// </summary>
    public int UniqueViewers { get; set; }

    /// <summary>
    /// Timestamp (UTC) of the most recent watch tick event recorded for this stream.
    /// </summary>
    public DateTime? LastEventAt { get; set; }
}
