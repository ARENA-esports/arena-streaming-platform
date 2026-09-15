using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Response returned after a team logo is uploaded and persisted.
/// </summary>
public record UploadTeamLogoResponse(
    [property: JsonPropertyName("logo_url")] string LogoUrl,
    [property: JsonPropertyName("team_id")] int TeamId
);
