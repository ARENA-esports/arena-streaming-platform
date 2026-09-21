namespace BattleEconomyService.DTOs;

public enum WatchTickStatus
{
    Success,
    RateLimited,       // AC2: Minimum interval not elapsed -> results in HTTP 429
    StreamNotLive,     // Extension point for SCRUM-118
    CapExceeded,       // Extension point for SCRUM-115
    Error
}

public class WatchTickResult
{
    public WatchTickStatus Status { get; init; }
    public int CoinsAwarded { get; init; }
    public int CurrentBalance { get; init; }
    public DateTime? LastTickAt { get; init; }
    public int? RemainingSeconds { get; init; }
    public string Message { get; init; } = string.Empty;

    public static WatchTickResult Awarded(int coinsAwarded, int currentBalance, DateTime lastTickAt) =>
        new()
        {
            Status = WatchTickStatus.Success,
            CoinsAwarded = coinsAwarded,
            CurrentBalance = currentBalance,
            LastTickAt = lastTickAt,
            Message = "Watch tick coins awarded successfully."
        };

    public static WatchTickResult TooEarly(int currentBalance, DateTime? lastTickAt, int remainingSeconds) =>
        new()
        {
            Status = WatchTickStatus.RateLimited,
            CoinsAwarded = 0,
            CurrentBalance = currentBalance,
            LastTickAt = lastTickAt,
            RemainingSeconds = remainingSeconds,
            Message = $"Anti-farm check failed. Minimum interval between awards has not elapsed. Try again in {remainingSeconds} seconds."
        };
}
