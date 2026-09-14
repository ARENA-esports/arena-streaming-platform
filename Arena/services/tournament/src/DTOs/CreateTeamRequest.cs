using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Request payload for registering a new team.
/// </summary>
public class CreateTeamRequest
{
    public const string HexColorRegex = @"^#[0-9A-Fa-f]{6}$";

    /// <summary>
    /// Unique name of the team.
    /// </summary>
    [Required(ErrorMessage = "Team name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Team name must be between 1 and 100 characters.")]
    [JsonPropertyName("team_name")]
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// 7-character hexadecimal color code starting with '#' (e.g. #FF0055).
    /// </summary>
    [Required(ErrorMessage = "Color hex code is required.")]
    [RegularExpression(HexColorRegex, ErrorMessage = "Color hex code must be a valid 7-character hexadecimal format starting with '#' (e.g., #FF0055).")]
    [JsonPropertyName("color_hex")]
    public string ColorHex { get; set; } = string.Empty;
}
