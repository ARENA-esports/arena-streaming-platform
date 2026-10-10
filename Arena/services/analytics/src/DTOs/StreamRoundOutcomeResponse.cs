namespace AnalyticsService.DTOs;

/// <summary>
/// Data transfer object representing round outcomes per stream.
/// Exposes read-model outcomes to tournament organizers viewing the battle stats dashboard (SCRUM-126).
/// </summary>
public class StreamRoundOutcomeResponse
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
}
