using System;
using System.Data;
using System.Threading.Tasks;
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

    public async Task<int> CreateTokenAsync(PasswordResetToken resetToken)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = @"
            INSERT INTO password_reset_tokens (user_id, token, expires_at, is_used)
            VALUES (@UserId, @Token, @ExpiresAt, @IsUsed);
            SELECT LAST_INSERT_ID();
        ";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", resetToken.UserId);
        command.Parameters.AddWithValue("@Token", resetToken.Token);
        command.Parameters.AddWithValue("@ExpiresAt", resetToken.ExpiresAt);
        command.Parameters.AddWithValue("@IsUsed", resetToken.IsUsed);

        var id = await command.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "SELECT reset_id, user_id, token, expires_at, created_at, is_used FROM password_reset_tokens WHERE token = @Token LIMIT 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Token", token);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PasswordResetToken
            {
                TokenId = reader.GetInt32(reader.GetOrdinal("reset_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                Token = reader.GetString(reader.GetOrdinal("token")),
                ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                IsUsed = reader.GetBoolean(reader.GetOrdinal("is_used"))
            };
        }
        return null;
    }

    public async Task<bool> MarkAsUsedAsync(string token)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "UPDATE password_reset_tokens SET is_used = TRUE WHERE token = @Token";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Token", token);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<int> InvalidateUserTokensAsync(int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "UPDATE password_reset_tokens SET is_used = TRUE WHERE user_id = @UserId AND is_used = FALSE";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        return await command.ExecuteNonQueryAsync();
    }
}