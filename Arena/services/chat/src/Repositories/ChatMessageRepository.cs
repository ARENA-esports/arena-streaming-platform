using System.Data;
using Dapper;
using MySqlConnector;
using ChatService.Entities;

namespace ChatService.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly IConfiguration _configuration;

    private string ConnectionString => _configuration.GetConnectionString("ChatDb")
        ?? throw new InvalidOperationException("Connection string ChatDb not found.");

    public ChatMessageRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    /// <inheritdoc />
    public async Task<long> InsertAsync(ChatMessage message)
    {
        using var connection = CreateConnection();

        const string sql = @"
            INSERT INTO chat_messages (team_id, user_id, username, content)
            VALUES (@TeamId, @UserId, @Username, @Content);
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            message.TeamId,
            message.UserId,
            message.Username,
            message.Content
        });

        return id;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> GetRecentByTeamAsync(int teamId, int limit = 50)
    {
        using var connection = CreateConnection();

        const string sql = @"
            SELECT * FROM (
                SELECT message_id, team_id, user_id, username, content, created_at
                FROM chat_messages
                WHERE team_id = @TeamId
                ORDER BY created_at DESC
                LIMIT @Limit
            ) AS recent
            ORDER BY created_at ASC;";

        return await connection.QueryAsync<ChatMessage>(sql, new { TeamId = teamId, Limit = limit });
    }
}
