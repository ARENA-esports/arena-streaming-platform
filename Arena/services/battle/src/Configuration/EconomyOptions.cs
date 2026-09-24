namespace BattleEconomyService.Configuration;

public class EconomyOptions
{
    public const string SectionName = "Economy";

    /// <summary>
    /// The minimum interval (in seconds) required between successful coin awards (AC1: 55 seconds).
    /// </summary>
    public int WatchTickIntervalSeconds { get; set; } = 55;

    /// <summary>
    /// The number of coins awarded per successful watch tick (AC1: 10 coins).
    /// </summary>
    public int WatchTickCoinsAwarded { get; set; } = 10;

    /// <summary>
    /// The rolling window duration (in seconds) used for the per-stream coin cap (SCRUM-115).
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int CoinCapWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum coins a viewer may earn per stream within a single rolling window (SCRUM-115).
    /// Default is 50 coins. When accumulated WATCH_TICK coins for (user_id, stream_id)
    /// reach this ceiling within the window, further ticks return HTTP 429.
    /// </summary>
    public int CoinCapPerWindow { get; set; } = 50;
}
