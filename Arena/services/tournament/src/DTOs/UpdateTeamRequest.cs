using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Request payload for updating team details.
/// At least one of team_name or color_hex should be provided.
/// </summary>
public class UpdateTeamRequest
{
    public const string HexColorRegex = @"^#[0-9A-Fa-f]{6}$";

    /// <summary>
    /// Updated unique name of the team.
    /// </summary>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Team name must be between 1 and 100 characters.")]
    [JsonPropertyName("team_name")]
    public string? TeamName { get; set; }

    /// <summary>
    /// Updated 7-character hexadecimal color code starting with '#' (e.g. #FF0055).
    /// </summary>
    [RegularExpression(HexColorRegex, ErrorMessage = "Color hex code must be a valid 7-character hexadecimal format starting with '#' (e.g., #FF0055).")]
    [JsonPropertyName("color_hex")]
    public string? ColorHex { get; set; }
}
