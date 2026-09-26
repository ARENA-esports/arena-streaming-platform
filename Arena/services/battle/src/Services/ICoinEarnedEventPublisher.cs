namespace BattleEconomyService.Services;

public record CoinEarnedEvent(
    int UserId,
    int WalletId,
    int Amount,
    int NewBalance,
    int? StreamId,
    DateTime AwardedAt
);

/// <summary>
/// Extension point for SCRUM-117: publishing CoinEarned Kafka event after successful earning.
/// </summary>
public interface ICoinEarnedEventPublisher
{
    Task PublishCoinEarnedAsync(CoinEarnedEvent evt);
}

public class NoOpCoinEarnedEventPublisher : ICoinEarnedEventPublisher
{
    public Task PublishCoinEarnedAsync(CoinEarnedEvent evt) => Task.CompletedTask;
}
