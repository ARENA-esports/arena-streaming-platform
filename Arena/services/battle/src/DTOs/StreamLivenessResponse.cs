using System.Text.Json.Serialization;

namespace BattleEconomyService.DTOs;

/// <summary>
/// Minimal contract representing stream status returned by StreamService (GET /api/streams/{streamId}).
/// Contains only the essential fields required to cross-check broadcast liveness for coin awards (SCRUM-118).
/// </summary>
public record StreamLivenessResponse
{
    [JsonPropertyName("streamId")]
    public int StreamId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
