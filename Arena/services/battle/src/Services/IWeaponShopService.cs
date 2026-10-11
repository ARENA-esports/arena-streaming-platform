using BattleEconomyService.DTOs;
using BattleEconomyService.Models;

namespace BattleEconomyService.Services;

public interface IWeaponShopService
{
    Task<List<WeaponDto>> GetWeaponsAsync();
    Task<AttackResponse> PurchaseAttackAsync(int userId, AttackRequest request);
    Task<List<BattleBar>> GetBarsForMatchAsync(int matchId);
}
