using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

public record TournamentResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("season_identifier")] string SeasonIdentifier,
    [property: JsonPropertyName("start_date")] DateTimeOffset StartDate,
    [property: JsonPropertyName("end_date")] DateTimeOffset EndDate,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] DateTime? CreatedAt = null,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt = null
);
