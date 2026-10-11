namespace BattleEconomyService.Models;

public class AttackLog
{
    public long AttackId { get; set; }
    public int UserId { get; set; }
    public int WeaponId { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public int CoinsSpent { get; set; }
    public int DamageDealt { get; set; }
    public DateTime CreatedAt { get; set; }
}
