namespace AnalyticsService.Configuration;

/// <summary>
/// Domain configuration options for engagement metrics and calculations in AnalyticsService.
/// Bound from the <c>"Analytics"</c> section of application configuration.
/// </summary>
public class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>
    /// Duration in seconds attributed to each watch tick represented by a CoinEarnedEvent.
    /// In BattleEconomyService, WatchTickIntervalSeconds is 55 seconds (anti-farm threshold),
    /// while the client-side viewer heartbeat ticks every 60 seconds (WATCH_HEARTBEAT_INTERVAL_MS).
    /// Defaults to 60 seconds.
    /// </summary>
    public int DefaultWatchTickDurationSeconds { get; set; } = 60;
}
