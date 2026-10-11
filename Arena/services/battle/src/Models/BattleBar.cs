namespace BattleEconomyService.Models;

/// <summary>
/// Represents a team's aggregated battle damage for a specific match.
/// Updated atomically via INSERT...ON DUPLICATE KEY UPDATE (SCRUM-120).
/// </summary>
public class BattleBar
{
    public int BarId { get; set; }
    public int MatchId { get; set; }
    public int TeamId { get; set; }
    public long TotalDamage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
