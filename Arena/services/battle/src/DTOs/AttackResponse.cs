namespace BattleEconomyService.DTOs;

/// <summary>
/// Response body for POST /api/economy/attack.
/// </summary>
public class AttackResponse
{
    public bool Success { get; set; }
    public long? AttackId { get; set; }
    public int CoinsSpent { get; set; }
    public int CurrentBalance { get; set; }
    public int DamageDealt { get; set; }
    public long TeamTotalDamage { get; set; }
    public string Message { get; set; } = string.Empty;
}
