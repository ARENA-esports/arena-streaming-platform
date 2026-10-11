using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public interface IWeaponRepository
{
    Task<List<Weapon>> GetActiveWeaponsAsync();
    Task<Weapon?> GetByIdAsync(int weaponId);
}
