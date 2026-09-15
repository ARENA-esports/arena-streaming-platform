using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TournamentService.DTOs;

/// <summary>
/// Request payload for adding a player to a team roster.
/// Supports both 'username' and 'player_name', and 'role' or 'position'.
/// </summary>
public class AddPlayerRequest
{
    /// <summary>
    /// Player username or display name.
    /// </summary>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 100 characters.")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Alternative alias for username (e.g. player_name).
    /// </summary>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Player name must be between 1 and 100 characters.")]
    [JsonPropertyName("player_name")]
    public string? PlayerName { get; set; }

    /// <summary>
    /// Optional role or position in the team (e.g. Captain, Duelist, Initiator).
    /// </summary>
    [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters.")]
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// Alternative alias for role (e.g. position).
    /// </summary>
    [StringLength(50, ErrorMessage = "Position cannot exceed 50 characters.")]
    [JsonPropertyName("position")]
    public string? Position { get; set; }

    /// <summary>
    /// Resolves the effective username from Username or PlayerName.
    /// </summary>
    [JsonIgnore]
    public string EffectiveUsername =>
        !string.IsNullOrWhiteSpace(Username) ? Username.Trim() :
        !string.IsNullOrWhiteSpace(PlayerName) ? PlayerName.Trim() :
        string.Empty;

    /// <summary>
    /// Resolves the effective role from Role or Position.
    /// </summary>
    [JsonIgnore]
    public string? EffectiveRole =>
        !string.IsNullOrWhiteSpace(Role) ? Role.Trim() :
        !string.IsNullOrWhiteSpace(Position) ? Position.Trim() :
        null;
}
