namespace BattleEconomyService.DTOs;

/// <summary>
/// WebSocket broadcast frame sent to all viewers connected to a match room.
/// </summary>
public class BattleBarBroadcastMessage
{
    /// <summary>
    /// Type of frame: "init" on initial connection snapshot, "battle_bar_update" on live attack updates.
    /// </summary>
    public string Type { get; set; } = "battle_bar_update";

    /// <summary>
    /// The match this update belongs to.
    /// </summary>
    public int MatchId { get; set; }

    /// <summary>
    /// Current aggregated damage bars for all teams in the match.
    /// </summary>
    public List<BattleBarDto> Bars { get; set; } = new();

    /// <summary>
    /// Optional metadata about the latest attack that caused this update.
    /// </summary>
    public AttackEventDto? LatestAttack { get; set; }

    /// <summary>
    /// UTC timestamp of when this broadcast was produced.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregated damage state for a team in a match.
/// </summary>
public class BattleBarDto
{
    public int TeamId { get; set; }
    public long TotalDamage { get; set; }
}

/// <summary>
/// Attack event details that influenced the battle bar update.
/// </summary>
public class AttackEventDto
{
    public int TeamId { get; set; }
    public int Damage { get; set; }
    public string WeaponName { get; set; } = string.Empty;
    public int WeaponId { get; set; }
}
