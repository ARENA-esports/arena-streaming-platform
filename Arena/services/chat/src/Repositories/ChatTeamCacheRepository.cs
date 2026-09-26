using System.Data;
using Dapper;
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

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    public async Task<ChatTeamCache?> GetByIdAsync(int teamId)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT team_id AS TeamId, team_name AS TeamName, team_color AS TeamColor 
            FROM chat_team_cache 
            WHERE team_id = @TeamId;";
        
        return await connection.QuerySingleOrDefaultAsync<ChatTeamCache>(sql, new { TeamId = teamId });
    }

    public async Task UpsertAsync(ChatTeamCache team)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO chat_team_cache (team_id, team_name, team_color)
            VALUES (@TeamId, @TeamName, @TeamColor)
            ON DUPLICATE KEY UPDATE 
                team_name = VALUES(team_name),
                team_color = VALUES(team_color);";
                
        await connection.ExecuteAsync(sql, new { team.TeamId, team.TeamName, team.TeamColor });
    }
}
