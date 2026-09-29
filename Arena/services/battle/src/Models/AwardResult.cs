namespace BattleEconomyService.Models;

public class AwardResult
{
    public bool Success { get; init; }
    public int CoinsAwarded { get; init; }
    public int CurrentBalance { get; init; }
    public DateTime? LastTickAt { get; init; }
    public int WalletId { get; init; }

    public static AwardResult Succeeded(int coinsAwarded, int currentBalance, DateTime lastTickAt, int walletId) =>
        new()
        {
            Success = true,
            CoinsAwarded = coinsAwarded,
            CurrentBalance = currentBalance,
            LastTickAt = lastTickAt,
            WalletId = walletId
        };

    public static AwardResult RateLimited(int currentBalance, DateTime? lastTickAt) =>
        new()
        {
            Success = false,
            CoinsAwarded = 0,
            CurrentBalance = currentBalance,
            LastTickAt = lastTickAt
        };
}
