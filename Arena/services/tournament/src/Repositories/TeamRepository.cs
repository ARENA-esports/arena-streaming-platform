using Microsoft.Extensions.Configuration;
using MySqlConnector;
using TournamentService.DTOs;
using TournamentService.Exceptions;

namespace TournamentService.Repositories;

/// <summary>
/// ADO.NET implementation of team and roster data access against MySQL database.
/// Read operations use MVCC snapshot isolation (non-locking) and optimize joins against indexed foreign keys.
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamResponse>> GetAllTeamsAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT team_id, team_name, color_hex, logo_url, created_at, updated_at
            FROM teams
            ORDER BY team_name ASC;";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var teams = new List<TeamResponse>();
        while (await reader.ReadAsync())
        {
            teams.Add(MapTeamResponse(reader));
        }

        return teams;
    }

    /// <inheritdoc />
    public async Task<TeamDetailsResponse?> GetTeamWithRosterAsync(int teamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Optimized LEFT JOIN against indexed foreign key idx_team_players_team_id
        // InnoDB MVCC consistent snapshot read ensures non-locking execution across concurrent services.
        const string sql = @"
            SELECT 
                t.team_id,
                t.team_name,
                t.color_hex,
                t.logo_url,
                t.created_at AS team_created_at,
                t.updated_at AS team_updated_at,
                p.player_id,
                p.username,
                p.role,
                p.is_active
            FROM teams t
            LEFT JOIN team_players p 
                ON t.team_id = p.team_id AND p.is_active = TRUE
            WHERE t.team_id = @TeamId
            ORDER BY p.player_id ASC;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamId", teamId);

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var id = reader.GetInt32("team_id");
        var name = reader.GetString("team_name");
        var color = reader.GetString("color_hex");

        var logoUrlOrdinal = reader.GetOrdinal("logo_url");
        var logoUrl = reader.IsDBNull(logoUrlOrdinal) ? null : reader.GetString(logoUrlOrdinal);

        var createdAtOrdinal = reader.GetOrdinal("team_created_at");
        var createdAt = reader.IsDBNull(createdAtOrdinal) ? null : (DateTime?)reader.GetDateTime(createdAtOrdinal);

        var updatedAtOrdinal = reader.GetOrdinal("team_updated_at");
        var updatedAt = reader.IsDBNull(updatedAtOrdinal) ? null : (DateTime?)reader.GetDateTime(updatedAtOrdinal);

        var roster = new List<PlayerResponse>();
        var playerIdOrdinal = reader.GetOrdinal("player_id");

        if (!reader.IsDBNull(playerIdOrdinal))
        {
            roster.Add(MapPlayerResponse(reader, id));
        }

        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(playerIdOrdinal))
            {
                roster.Add(MapPlayerResponse(reader, id));
            }
        }

        return new TeamDetailsResponse(id, name, color, logoUrl, roster, createdAt, updatedAt);
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

    private static PlayerResponse MapPlayerResponse(MySqlDataReader reader, int teamId)
    {
        var roleOrdinal = reader.GetOrdinal("role");
        return new PlayerResponse(
            reader.GetInt32("player_id"),
            teamId,
            reader.GetString("username"),
            reader.IsDBNull(roleOrdinal) ? null : reader.GetString(roleOrdinal),
            reader.GetBoolean("is_active")
        );
    }
}
