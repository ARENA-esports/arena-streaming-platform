namespace BattleEconomyService.DTOs;

/// <summary>
/// Deserialized response payload from StreamService GET api/matches/{id}.
/// </summary>
public class MatchDetailsResponse
{
    public int MatchId { get; set; }
    public int TournamentId { get; set; }
    public int TeamAId { get; set; }
    public int TeamBId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? WinnerTeamId { get; set; }
}
