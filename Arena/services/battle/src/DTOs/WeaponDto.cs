namespace BattleEconomyService.DTOs;

/// <summary>
/// Response shape for GET /api/economy/weapons — a single weapon in the catalog.
/// </summary>
public class WeaponDto
{
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }
    public int Damage { get; set; }
    public string IconKey { get; set; } = "sword";
}
