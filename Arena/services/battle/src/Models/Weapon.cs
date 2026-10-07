namespace BattleEconomyService.Models;

public class Weapon
{
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }
    public int Damage { get; set; }
    public string IconKey { get; set; } = "sword";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
