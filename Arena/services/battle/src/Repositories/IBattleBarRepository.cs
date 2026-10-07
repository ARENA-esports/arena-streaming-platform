using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

/// <summary>
/// Repository for battle bar damage aggregation (SCRUM-120).
/// </summary>
public interface IBattleBarRepository
{
    /// <summary>
    /// Atomically adds damage to a team's battle bar using
    /// INSERT ... ON DUPLICATE KEY UPDATE total_damage = total_damage + damage.
    /// Auto-initialises the bar if it does not yet exist.
    /// </summary>
    /// <returns>The updated total_damage for the team after the increment.</returns>
    Task<long> ApplyDamageAtomicAsync(int matchId, int teamId, int damage);

    /// <summary>
    /// Retrieves the current battle bar state for every team in a match.
    /// </summary>
    Task<List<BattleBar>> GetBarsForMatchAsync(int matchId);
}
