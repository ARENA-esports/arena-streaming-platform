using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using UserService.Entities;

namespace UserService.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly IConfiguration _configuration;
    private string ConnectionString => _configuration.GetConnectionString("UserDb")
        ?? throw new InvalidOperationException("Connection string UserDb not found.");

    public PasswordResetRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    public async Task<int> CreateTokenAsync(PasswordResetToken resetToken)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO password_reset_tokens (user_id, token, expires_at, is_used)
            VALUES (@UserId, @Token, @ExpiresAt, @IsUsed);
            SELECT LAST_INSERT_ID();
        ";

        var id = await connection.ExecuteScalarAsync<ulong>(sql, new
        {
            UserId = resetToken.UserId,
            Token = resetToken.Token,
            ExpiresAt = resetToken.ExpiresAt,
            IsUsed = resetToken.IsUsed
        });

        return (int)id;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM password_reset_tokens WHERE token = @Token LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<PasswordResetToken>(sql, new { Token = token });
    }

    public async Task<bool> MarkAsUsedAsync(string token)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE password_reset_tokens SET is_used = TRUE WHERE token = @Token";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Token = token });
        return rowsAffected > 0;
    }

    public async Task<int> InvalidateUserTokensAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE password_reset_tokens SET is_used = TRUE WHERE user_id = @UserId AND is_used = FALSE";
        return await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
