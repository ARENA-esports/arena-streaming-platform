namespace Arena.Shared.EventContracts;

/// <summary>
/// Kafka event contract published by BattleEconomyService whenever a viewer
/// successfully earns coins from a watch tick (SCRUM-117).
/// This is the agreed, cross-service payload shape for the
/// <c>arena.coin-earned</c> topic.
/// </summary>
/// <param name="UserId">Identifier of the viewer who earned the coins.</param>
/// <param name="MatchId">
/// Identifier of the stream/match being watched.
/// <c>null</c> when no stream context was provided with the watch-tick request.
/// </param>
/// <param name="Amount">Number of coins awarded in this tick.</param>
/// <param name="TimestampUtc">
/// UTC timestamp at which the award was committed to the database.
/// </param>
public record CoinEarnedEvent(
    int UserId,
    int? MatchId,
    int Amount,
    DateTime TimestampUtc
);
