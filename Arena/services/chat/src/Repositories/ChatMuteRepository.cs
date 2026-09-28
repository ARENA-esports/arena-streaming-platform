using System.Data;
using Dapper;
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

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    /// <inheritdoc />
    public async Task<long> InsertAsync(ChatMute mute)
    {
        using var connection = CreateConnection();

        const string sql = @"
            INSERT INTO chat_mutes (user_id, muted_by, reason, expires_at)
            VALUES (@UserId, @MutedBy, @Reason, @ExpiresAt);
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            mute.UserId,
            mute.MutedBy,
            mute.Reason,
            mute.ExpiresAt
        });

        return id;
    }

    /// <inheritdoc />
    public async Task<bool> IsUserMutedAsync(int userId)
    {
        using var connection = CreateConnection();

        const string sql = @"
            SELECT COUNT(1) FROM chat_mutes
            WHERE user_id = @UserId AND expires_at > UTC_TIMESTAMP()";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        return count > 0;
    }

    /// <inheritdoc />
    public async Task<ChatMute?> GetActiveMuteAsync(int userId)
    {
        using var connection = CreateConnection();

        const string sql = @"
            SELECT mute_id, user_id, muted_by, reason, muted_at, expires_at
            FROM chat_mutes
            WHERE user_id = @UserId AND expires_at > UTC_TIMESTAMP()
            ORDER BY expires_at DESC
            LIMIT 1";

        return await connection.QueryFirstOrDefaultAsync<ChatMute>(sql, new { UserId = userId });
    }
}
