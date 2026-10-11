using BattleEconomyService.DTOs;

namespace BattleEconomyService.Services;

public interface IWeaponShopService
{
    Task<List<WeaponDto>> GetWeaponsAsync();
    Task<AttackResponse> PurchaseAttackAsync(int userId, AttackRequest request);
}
