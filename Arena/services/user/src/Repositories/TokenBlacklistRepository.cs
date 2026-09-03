using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace UserService.Repositories;

public class TokenBlacklistRepository : ITokenBlacklistRepository
{
    private readonly IConfiguration _configuration;
    private string ConnectionString => _configuration.GetConnectionString("UserDb")
        ?? throw new InvalidOperationException("Connection string UserDb not found.");

    public TokenBlacklistRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    public async Task RevokeTokenAsync(string jti, int? userId, DateTime expiresAt)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO revoked_tokens (jti, user_id, expires_at)
            VALUES (@Jti, @UserId, @ExpiresAt)
            ON DUPLICATE KEY UPDATE expires_at = @ExpiresAt;
        ";

        await connection.ExecuteAsync(sql, new
        {
            Jti = jti,
            UserId = userId,
            ExpiresAt = expiresAt
        });
    }

    public async Task<bool> IsTokenRevokedAsync(string jti)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT COUNT(1) FROM revoked_tokens WHERE jti = @Jti AND expires_at > NOW() LIMIT 1;";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Jti = jti });
        return count > 0;
    }
}
