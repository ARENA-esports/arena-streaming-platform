namespace AnalyticsService.DTOs;

/// <summary>
/// Data transfer object representing aggregated attack and battle metrics per stream and team.
/// Exposes read-model metrics to tournament organizers viewing the battle stats dashboard (SCRUM-126).
/// </summary>
public class StreamTeamBattleResponse
{
    /// <summary>
    /// Stream / match identifier.
    /// </summary>
    public int StreamId { get; set; }

    /// <summary>
    /// Team identifier.
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// Denormalized team name.
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// Total attack actions executed by/for this team.
    /// </summary>
    public int TotalAttacks { get; set; }

    /// <summary>
    /// Total damage points dealt by attacks.
    /// </summary>
    public long TotalDamageDealt { get; set; }

    /// <summary>
    /// Total coins spent on attacks by viewers supporting this team.
    /// </summary>
    public long TotalCoinsSpent { get; set; }

    /// <summary>
    /// Number of battle rounds won by this team in this stream.
    /// </summary>
    public int RoundsWon { get; set; }

    /// <summary>
    /// Number of battle rounds lost by this team in this stream.
    /// </summary>
    public int RoundsLost { get; set; }

    /// <summary>
    /// Timestamp (UTC) of the most recent attack recorded.
    /// </summary>
    public DateTime? LastAttackAt { get; set; }
}
