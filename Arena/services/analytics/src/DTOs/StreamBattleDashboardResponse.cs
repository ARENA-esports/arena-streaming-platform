namespace AnalyticsService.DTOs;

/// <summary>
/// Aggregated dashboard response representing complete battle statistics and round outcomes for a specific stream.
/// Queried by tournament organizers viewing the battle stats dashboard (SCRUM-126).
/// </summary>
public class StreamBattleDashboardResponse
{
    /// <summary>
    /// Stream / match identifier.
    /// </summary>
    public int StreamId { get; set; }

    /// <summary>
    /// Aggregated attack and battle metrics per participating team.
    /// </summary>
    public IReadOnlyList<StreamTeamBattleResponse> Teams { get; set; } = Array.Empty<StreamTeamBattleResponse>();

    /// <summary>
    /// Sequential round outcome history for this stream.
    /// </summary>
    public IReadOnlyList<StreamRoundOutcomeResponse> Rounds { get; set; } = Array.Empty<StreamRoundOutcomeResponse>();
}
