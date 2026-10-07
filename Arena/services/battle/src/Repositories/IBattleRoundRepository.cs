using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

/// <summary>
/// Repository managing match round lifecycles and atomic round-active flag transitions (SCRUM-122).
/// </summary>
public interface IBattleRoundRepository
{
    /// <summary>
    /// Retrieves the currently active round for a match, if one exists.
    /// </summary>
    Task<BattleRound?> GetActiveRoundAsync(int matchId);

    /// <summary>
    /// Retrieves the currently active round for a match, or creates Round 1 if none exists.
    /// </summary>
    Task<BattleRound> GetOrCreateActiveRoundAsync(int matchId, long targetDamage);

    /// <summary>
    /// Atomically flips round_active from TRUE to FALSE for a round if it has not already been flipped.
    /// Uses MySQL InnoDB row-level locking to guarantee exactly one caller succeeds under concurrent execution.
    /// </summary>
    /// <param name="roundId">The round to end.</param>
    /// <param name="winningTeamId">The team that dealt the decisive damage to hit 100%.</param>
    /// <returns>True if this process successfully flipped the flag; false if it was already flipped.</returns>
    Task<bool> TryFlipRoundActiveAsync(int roundId, int winningTeamId);

    /// <summary>
    /// Resets match battle bar damage totals to 0 and initializes the next active round in a single transaction.
    /// Should only be invoked by the single process that successfully flipped the round-active flag.
    /// </summary>
    Task<BattleRound> ResetBarsAndStartNextRoundAsync(int matchId, int nextRoundNumber, long targetDamage);

    /// <summary>
    /// Retrieves past completed rounds (round_active = FALSE) for a match in reverse-chronological order (SCRUM-123).
    /// If no rounds have completed yet, returns an empty list.
    /// </summary>
    Task<List<BattleRound>> GetRoundHistoryAsync(int matchId);
}
