using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using ChatService.Entities;

namespace ChatService.Repositories;

public class ChatMuteRepository : IChatMuteRepository
{
    private readonly IConfiguration _configuration;

    private string ConnectionString => _configuration.GetConnectionString("ChatDb")
        ?? throw new InvalidOperationException("Connection string ChatDb not found.");

    public ChatMuteRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<long> InsertAsync(ChatMute mute)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO chat_mutes (user_id, muted_by, reason, expires_at)
            VALUES (@UserId, @MutedBy, @Reason, @ExpiresAt);
            SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", mute.UserId);
        command.Parameters.AddWithValue("@MutedBy", mute.MutedBy);
        command.Parameters.AddWithValue("@Reason", mute.Reason);
        command.Parameters.AddWithValue("@ExpiresAt", mute.ExpiresAt);

        var id = await command.ExecuteScalarAsync();
        return Convert.ToInt64(id);
    }

    public async Task<bool> IsUserMutedAsync(int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT COUNT(1) FROM chat_mutes
            WHERE user_id = @UserId AND expires_at > UTC_TIMESTAMP()";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        var count = await command.ExecuteScalarAsync();
        return Convert.ToInt32(count) > 0;
    }

    public async Task<ChatMute?> GetActiveMuteAsync(int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT mute_id, user_id, muted_by, reason, muted_at, expires_at
            FROM chat_mutes
            WHERE user_id = @UserId AND expires_at > UTC_TIMESTAMP()
            ORDER BY expires_at DESC
            LIMIT 1";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ChatMute
            {
                MuteId = reader.GetInt64(reader.GetOrdinal("mute_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                MutedBy = reader.GetInt32(reader.GetOrdinal("muted_by")),
                Reason = reader.GetString(reader.GetOrdinal("reason")),
                MutedAt = reader.GetDateTime(reader.GetOrdinal("muted_at")),
                ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at"))
            };
        }
        return null;
    }
}
