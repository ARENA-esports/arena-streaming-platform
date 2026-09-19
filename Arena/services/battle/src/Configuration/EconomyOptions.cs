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
}
