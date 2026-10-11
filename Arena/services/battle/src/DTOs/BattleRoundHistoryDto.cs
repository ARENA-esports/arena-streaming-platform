namespace BattleEconomyService.DTOs;

/// <summary>
/// DTO representing a completed battle round outcome in history log (SCRUM-123).
/// </summary>
public class BattleRoundHistoryDto
{
    /// <summary>
    /// Unique database round ID.
    /// </summary>
    public int RoundId { get; set; }

    /// <summary>
    /// ID of the match this round belongs to.
    /// </summary>
    public int MatchId { get; set; }

    /// <summary>
    /// Round sequence number within the match.
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Team ID that won this round.
    /// </summary>
    public int? WinningTeamId { get; set; }

    /// <summary>
    /// Target damage threshold required to win the round.
    /// </summary>
    public long TargetDamage { get; set; }

    /// <summary>
    /// Snapshot of team battle bar totals at the moment the round completed.
    /// </summary>
    public List<BattleBarDto> FinalBarState { get; set; } = new();

    /// <summary>
    /// UTC timestamp of when this round started.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp of when this round completed.
    /// </summary>
    public DateTime? EndedAt { get; set; }
}
