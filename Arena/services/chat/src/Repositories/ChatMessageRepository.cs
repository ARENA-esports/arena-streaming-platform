using System.Data;
using Microsoft.Extensions.Configuration;
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

    public async Task<long> InsertAsync(ChatMessage message)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO chat_messages (team_id, user_id, username, content)
            VALUES (@TeamId, @UserId, @Username, @Content);
            SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamId", message.TeamId);
        command.Parameters.AddWithValue("@UserId", message.UserId);
        command.Parameters.AddWithValue("@Username", message.Username);
        command.Parameters.AddWithValue("@Content", message.Content);

        var id = await command.ExecuteScalarAsync();
        return Convert.ToInt64(id);
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentByTeamAsync(int teamId, int limit = 50)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT * FROM (
                SELECT message_id, team_id, user_id, username, content, created_at
                FROM chat_messages
                WHERE team_id = @TeamId
                ORDER BY created_at DESC
                LIMIT @Limit
            ) AS recent
            ORDER BY created_at ASC;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeamId", teamId);
        command.Parameters.AddWithValue("@Limit", limit);

        using var reader = await command.ExecuteReaderAsync();
        var list = new List<ChatMessage>();
        while (await reader.ReadAsync())
        {
            list.Add(new ChatMessage
            {
                MessageId = reader.GetInt64(reader.GetOrdinal("message_id")),
                TeamId = reader.GetInt32(reader.GetOrdinal("team_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                Username = reader.GetString(reader.GetOrdinal("username")),
                Content = reader.GetString(reader.GetOrdinal("content")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }
        return list;
    }
}
