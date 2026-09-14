using Microsoft.Extensions.Configuration;
using MySqlConnector;
using TournamentService.DTOs;
using TournamentService.Exceptions;

namespace TournamentService.Repositories;

/// <summary>
/// ADO.NET implementation of team data access against MySQL database.
/// </summary>
public class TeamRepository : ITeamRepository
{
    private readonly string _connectionString;

    public TeamRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    /// <inheritdoc />
    public async Task<int> CreateTeamAsync(string teamName, string colorHex)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO teams (team_name, color_hex)
            VALUES (@TeamName, @ColorHex);
            SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamName", teamName);
        command.Parameters.AddWithValue("@ColorHex", colorHex);

        try
        {
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (MySqlException ex) when (ex.Number == 1062) // MySQL duplicate key error code
        {
            throw new TeamConflictException(teamName, ex);
        }
    }

    /// <inheritdoc />
    public async Task<TeamResponse?> GetTeamByIdAsync(int teamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT team_id, team_name, color_hex, created_at, updated_at
            FROM teams
            WHERE team_id = @TeamId
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamId", teamId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapTeamResponse(reader);
        }

        return null;
    }

    private static TeamResponse MapTeamResponse(MySqlDataReader reader)
    {
        return new TeamResponse(
            reader.GetInt32("team_id"),
            reader.GetString("team_name"),
            reader.GetString("color_hex"),
            reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime("created_at"),
            reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime("updated_at")
        );
    }
}
