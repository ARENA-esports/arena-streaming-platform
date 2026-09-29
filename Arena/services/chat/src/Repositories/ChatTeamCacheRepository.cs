using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using ChatService.Entities;

namespace ChatService.Repositories;

public class ChatTeamCacheRepository : IChatTeamCacheRepository
{
    private readonly IConfiguration _configuration;

    private string ConnectionString => _configuration.GetConnectionString("ChatDb")
        ?? throw new InvalidOperationException("Connection string ChatDb not found.");

    public ChatTeamCacheRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ChatTeamCache?> GetByIdAsync(int teamId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = @"
            SELECT team_id, team_name, team_color 
            FROM chat_team_cache 
            WHERE team_id = @TeamId;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamId", teamId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ChatTeamCache
            {
                TeamId = reader.GetInt32(reader.GetOrdinal("team_id")),
                TeamName = reader.GetString(reader.GetOrdinal("team_name")),
                TeamColor = reader.GetString(reader.GetOrdinal("team_color"))
            };
        }
        return null;
    }

    public async Task UpsertAsync(ChatTeamCache team)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = @"
            INSERT INTO chat_team_cache (team_id, team_name, team_color)
            VALUES (@TeamId, @TeamName, @TeamColor)
            ON DUPLICATE KEY UPDATE 
                team_name = VALUES(team_name),
                team_color = VALUES(team_color);";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamId", team.TeamId);
        command.Parameters.AddWithValue("@TeamName", team.TeamName);
        command.Parameters.AddWithValue("@TeamColor", team.TeamColor);

        await command.ExecuteNonQueryAsync();
    }
}
