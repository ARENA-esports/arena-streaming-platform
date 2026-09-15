using TournamentService.DTOs;

namespace TournamentService.Repositories;

/// <summary>
/// Data access operations for teams.
/// </summary>
public interface ITeamRepository
{
    /// <summary>
    /// Inserts a new team record into MySQL via ADO.NET.
    /// Throws TeamConflictException if a team with the same team_name already exists.
    /// </summary>
    /// <param name="teamName">Unique team name.</param>
    /// <param name="colorHex">Valid 7-character hex color code (e.g. #FF0055).</param>
    /// <returns>Assigned team_id.</returns>
    Task<int> CreateTeamAsync(string teamName, string colorHex);

    /// <summary>
    /// Retrieves team details by its primary identifier.
    /// </summary>
    /// <param name="teamId">Team identifier.</param>
    /// <returns>Team record or null if not found.</returns>
    Task<TeamResponse?> GetTeamByIdAsync(int teamId);

    /// <summary>
    /// Updates the logo URL for a specified team via ADO.NET.
    /// </summary>
    /// <param name="teamId">Team identifier.</param>
    /// <param name="logoUrl">Public storage URI of the uploaded team logo.</param>
    /// <returns>True if a team record was updated, false otherwise.</returns>
    Task<bool> UpdateTeamLogoAsync(int teamId, string logoUrl);

    /// <summary>
    /// Updates team details (team_name and/or color_hex) via an atomic ADO.NET query.
    /// Throws TeamConflictException if the new team_name collides with another team.
    /// </summary>
    /// <param name="teamId">Team identifier.</param>
    /// <param name="teamName">Updated team name, or null to preserve existing name.</param>
    /// <param name="colorHex">Updated color hex, or null to preserve existing color.</param>
    /// <returns>True if a record was updated, false otherwise.</returns>
    Task<bool> UpdateTeamAsync(int teamId, string? teamName, string? colorHex);

    /// <summary>
    /// Retrieves all registered teams ordered alphabetically.
    /// </summary>
    /// <returns>A read-only list of all teams.</returns>
    Task<IReadOnlyList<TeamResponse>> GetAllTeamsAsync();

    /// <summary>
    /// Retrieves a team by its unique identifier along with its active roster of players.
    /// Uses an optimized joined query against indexed foreign keys with non-locking reads.
    /// </summary>
    /// <param name="teamId">Unique team identifier.</param>
    /// <returns>Team details with active roster, or null if the team does not exist.</returns>
    Task<TeamDetailsResponse?> GetTeamWithRosterAsync(int teamId);

    /// <summary>
    /// Adds a player to a team's roster using a parameterized ADO.NET query.
    /// </summary>
    /// <param name="teamId">Team identifier.</param>
    /// <param name="username">Player username.</param>
    /// <param name="role">Optional player role or position.</param>
    /// <returns>Assigned player_id.</returns>
    Task<int> AddPlayerToTeamAsync(int teamId, string username, string? role);

    /// <summary>
    /// Removes a player from a team's roster using a parameterized ADO.NET query.
    /// </summary>
    /// <param name="teamId">Team identifier.</param>
    /// <param name="playerId">Player identifier.</param>
    /// <returns>True if a player was removed, false otherwise.</returns>
    Task<bool> RemovePlayerFromTeamAsync(int teamId, int playerId);
}
