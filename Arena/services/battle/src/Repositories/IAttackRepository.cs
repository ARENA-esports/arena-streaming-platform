using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public interface IAttackRepository
{
    Task<long> RecordAttackAsync(AttackLog attackLog);
}
