using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Response payload representing a registered team.
/// </summary>
public record TeamResponse(
    [property: JsonPropertyName("team_id")] int TeamId,
    [property: JsonPropertyName("team_name")] string TeamName,
    [property: JsonPropertyName("color_hex")] string ColorHex,
    [property: JsonPropertyName("logo_url")] string? LogoUrl = null,
    [property: JsonPropertyName("created_at")] DateTime? CreatedAt = null,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt = null
);
