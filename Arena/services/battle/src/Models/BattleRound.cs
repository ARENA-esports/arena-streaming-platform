namespace BattleEconomyService.Models;

/// <summary>
/// Domain model for battle_rounds representing a match round with an atomic round_active flag (SCRUM-122).
/// </summary>
public class BattleRound
{
    public int RoundId { get; set; }
    public int MatchId { get; set; }
    public int RoundNumber { get; set; } = 1;
    public long TargetDamage { get; set; } = 100;
    public int? WinningTeamId { get; set; }
    public string? FinalBarState { get; set; }
    public bool RoundActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
