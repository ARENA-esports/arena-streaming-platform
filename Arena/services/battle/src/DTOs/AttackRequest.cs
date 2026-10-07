namespace BattleEconomyService.DTOs;

/// <summary>
/// Request body for POST /api/economy/attack — purchase a weapon attack.
/// </summary>
public class AttackRequest
{
    public int WeaponId { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
}
