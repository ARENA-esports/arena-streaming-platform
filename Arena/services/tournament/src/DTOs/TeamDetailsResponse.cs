using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Detailed team response containing faction branding and active roster players.
/// </summary>
public record TeamDetailsResponse(
    [property: JsonPropertyName("team_id")] int TeamId,
    [property: JsonPropertyName("team_name")] string TeamName,
    [property: JsonPropertyName("color_hex")] string ColorHex,
    [property: JsonPropertyName("logo_url")] string? LogoUrl,
    [property: JsonPropertyName("roster")] IReadOnlyList<PlayerResponse> Roster,
    [property: JsonPropertyName("created_at")] DateTime? CreatedAt = null,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt = null
);
