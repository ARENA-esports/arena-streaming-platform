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
            SELECT team_id, team_name, color_hex, logo_url, created_at, updated_at
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

    /// <inheritdoc />
    public async Task<bool> UpdateTeamLogoAsync(int teamId, string logoUrl)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE teams
            SET logo_url = @LogoUrl
            WHERE team_id = @TeamId;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LogoUrl", logoUrl);
        command.Parameters.AddWithValue("@TeamId", teamId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private static TeamResponse MapTeamResponse(MySqlDataReader reader)
    {
        var logoUrlOrdinal = reader.GetOrdinal("logo_url");
        var createdAtOrdinal = reader.GetOrdinal("created_at");
        var updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new TeamResponse(
            reader.GetInt32("team_id"),
            reader.GetString("team_name"),
            reader.GetString("color_hex"),
            reader.IsDBNull(logoUrlOrdinal) ? null : reader.GetString(logoUrlOrdinal),
            reader.IsDBNull(createdAtOrdinal) ? null : reader.GetDateTime(createdAtOrdinal),
            reader.IsDBNull(updatedAtOrdinal) ? null : reader.GetDateTime(updatedAtOrdinal)
        );
    }
}
