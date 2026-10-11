using BattleEconomyService.Models;

namespace BattleEconomyService.DTOs;

/// <summary>
/// Domain result representing match validation from StreamService (SCRUM-119 / Security Finding 2).
/// Decouples HTTP status codes and transient network errors from business logic.
/// </summary>
public record MatchDetailsResult
{
    public int MatchId { get; init; }
    public bool Found { get; init; }
    public bool IsLive { get; init; }
    public int TeamAId { get; init; }
    public int TeamBId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static MatchDetailsResult Live(int matchId, int teamAId, int teamBId, string status) =>
        new()
        {
            MatchId = matchId,
            Found = true,
            IsLive = true,
            TeamAId = teamAId,
            TeamBId = teamBId,
            Status = status,
            IsSuccess = true
        };

    public static MatchDetailsResult NotLive(int matchId, int teamAId, int teamBId, string status) =>
        new()
        {
            MatchId = matchId,
            Found = true,
            IsLive = false,
            TeamAId = teamAId,
            TeamBId = teamBId,
            Status = status,
            IsSuccess = true
        };

    public static MatchDetailsResult NotFound(int matchId) =>
        new()
        {
            MatchId = matchId,
            Found = false,
            IsLive = false,
            Status = "NotFound",
            IsSuccess = false,
            ErrorMessage = $"Match with ID {matchId} not found."
        };

    public static MatchDetailsResult Failed(int matchId, string error) =>
        new()
        {
            MatchId = matchId,
            Found = false,
            IsLive = false,
            Status = "Unavailable",
            IsSuccess = false,
            ErrorMessage = error
        };
}
