using System.Data;
using Dapper;
using MySqlConnector;
using UserService.Entities;

namespace UserService.Repositories;

public class EmailVerificationRepository : IEmailVerificationRepository
{
    private readonly IConfiguration _configuration;
    private string ConnectionString => _configuration.GetConnectionString("UserDb")
        ?? throw new InvalidOperationException("Connection string UserDb not found.");

    public EmailVerificationRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    public async Task<int> CreateTokenAsync(EmailVerificationToken token)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO email_verification_tokens (user_id, token, expires_at, is_used)
            VALUES (@UserId, @Token, @ExpiresAt, @IsUsed);
            SELECT LAST_INSERT_ID();
        ";

        var id = await connection.ExecuteScalarAsync<ulong>(sql, new
        {
            UserId = token.UserId,
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            IsUsed = token.IsUsed
        });

        return (int)id;
    }

    public async Task<EmailVerificationToken?> GetByTokenAsync(string token)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM email_verification_tokens WHERE token = @Token LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<EmailVerificationToken>(sql, new { Token = token });
    }

    public async Task<bool> MarkAsUsedAsync(string token)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE email_verification_tokens SET is_used = TRUE WHERE token = @Token";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Token = token });
        return rowsAffected > 0;
    }

    public async Task<int> InvalidateUserTokensAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE email_verification_tokens SET is_used = TRUE WHERE user_id = @UserId AND is_used = FALSE";
        return await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
