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

    /// <summary>
    /// Indicates whether this attack caused the battle bar to hit 100% and concluded the round (SCRUM-122).
    /// </summary>
    public bool RoundEnded { get; set; }

    /// <summary>
    /// The team id that won the round if RoundEnded is true.
    /// </summary>
    public int? WinningTeamId { get; set; }

    /// <summary>
    /// The round number of the match.
    /// </summary>
    public int RoundNumber { get; set; } = 1;
}
