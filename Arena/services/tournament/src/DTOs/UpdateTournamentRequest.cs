using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

public class UpdateTournamentRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Season identifier is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Season identifier must be between 1 and 100 characters.")]
    [JsonPropertyName("season_identifier")]
    public string SeasonIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [JsonPropertyName("start_date")]
    public DateTimeOffset StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [JsonPropertyName("end_date")]
    public DateTimeOffset EndDate { get; set; }
}
