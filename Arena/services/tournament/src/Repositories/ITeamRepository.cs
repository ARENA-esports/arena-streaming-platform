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
}
