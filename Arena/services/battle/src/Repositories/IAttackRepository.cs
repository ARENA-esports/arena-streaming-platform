using BattleEconomyService.DTOs;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public interface IAttackRepository
{
    Task<long> RecordAttackAsync(AttackLog attackLog);

    /// <summary>
    /// Executes the wallet deduction, attack logging, and coin transaction recording
    /// atomically within a single database transaction (Finding 1).
    /// </summary>
    Task<AtomicAttackResult> ExecuteAttackTransactionAsync(AttackLog attackLog, int weaponCost);
}
