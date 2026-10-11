namespace AnalyticsService.Entities;

/// <summary>
/// Read-model entity representing aggregated battle and attack metrics per stream and team.
/// Populated in arena_analytics_db for organizer dashboard reporting (SCRUM-126).
/// </summary>
public class StreamTeamBattleSummary
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

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
