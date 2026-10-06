namespace AnalyticsService.Entities;

/// <summary>
/// Read-model entity representing round outcome history per stream and participating teams.
/// Stored in arena_analytics_db for organizer dashboard reporting (SCRUM-126).
/// </summary>
public class StreamRoundOutcome
{
    /// <summary>
    /// Stream / match identifier.
    /// </summary>
    public int StreamId { get; set; }

    /// <summary>
    /// Sequential round number within the match.
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Winning team identifier.
    /// </summary>
    public int WinningTeamId { get; set; }

    /// <summary>
    /// Denormalized name of the winning team.
    /// </summary>
    public string WinningTeamName { get; set; } = string.Empty;

    /// <summary>
    /// Team A identifier.
    /// </summary>
    public int TeamAId { get; set; }

    /// <summary>
    /// Team B identifier.
    /// </summary>
    public int TeamBId { get; set; }

    /// <summary>
    /// Total attacks launched by Team A in this round.
    /// </summary>
    public int TeamAAttacks { get; set; }

    /// <summary>
    /// Total attacks launched by Team B in this round.
    /// </summary>
    public int TeamBAttacks { get; set; }

    /// <summary>
    /// Total damage dealt by Team A in this round.
    /// </summary>
    public int TeamADamage { get; set; }

    /// <summary>
    /// Total damage dealt by Team B in this round.
    /// </summary>
    public int TeamBDamage { get; set; }

    /// <summary>
    /// Timestamp (UTC) when the round completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
