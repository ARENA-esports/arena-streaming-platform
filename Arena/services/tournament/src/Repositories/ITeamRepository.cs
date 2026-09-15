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
}
