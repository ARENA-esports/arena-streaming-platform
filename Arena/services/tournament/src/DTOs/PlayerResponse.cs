using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Represents a competing roster player belonging to a team.
/// </summary>
public record PlayerResponse(
    [property: JsonPropertyName("player_id")] int PlayerId,
    [property: JsonPropertyName("team_id")] int TeamId,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("role")] string? Role = null,
    [property: JsonPropertyName("is_active")] bool IsActive = true
);
