using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MySqlConnector;
using UserService.Entities;
using UserService.Models;

namespace UserService.Repositories;

public class ExternalAuthRepository : IExternalAuthRepository
{
    private readonly string _connectionString;

    public ExternalAuthRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("UserDb")
            ?? throw new InvalidOperationException("UserDb connection string not configured.");
    }

    public async Task<User?> GetUserByTwitchIdAsync(string twitchUserId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT u.user_id, u.username, u.email, u.password_hash, u.role, 
                   u.email_verified, u.avatar_url, u.display_name, u.created_at, u.updated_at
            FROM users u
            INNER JOIN user_auth_providers p ON u.user_id = p.user_id
            WHERE p.provider = 'twitch' AND p.provider_user_id = @twitchUserId
            LIMIT 1;";

        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@twitchUserId", twitchUserId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                UserId = reader.GetInt32("user_id"),
                Username = reader.GetString("username"),
                Email = reader.GetString("email"),
                PasswordHash = reader.IsDBNull(reader.GetOrdinal("password_hash")) ? null : reader.GetString("password_hash"),
                Role = reader.GetString("role"),
                EmailVerified = reader.GetBoolean("email_verified"),
                AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString("avatar_url"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        return null;
    }

    public async Task<User> CreateUserFromTwitchAsync(TwitchUserInfo info)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Sanitize and generate unique username
            var baseName = Regex.Replace(info.Username, @"[^a-zA-Z0-9_]", "");
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "TwitchUser";
            if (baseName.Length > 40) baseName = baseName[..40];

            var uniqueUsername = baseName;
            var counter = 1;
            while (await UsernameExistsAsync(connection, transaction, uniqueUsername))
            {
                uniqueUsername = $"{baseName}_{RandomNumberGenerator.GetInt32(1000, 9999)}";
                counter++;
                if (counter > 10) uniqueUsername = $"User_{Guid.NewGuid().ToString("N")[..8]}";
            }

            // 2. Insert into users table
            const string insertUserSql = @"
                INSERT INTO users (username, email, password_hash, role, email_verified, avatar_url)
                VALUES (@username, @email, NULL, 'Viewer', TRUE, @avatarUrl);
                SELECT LAST_INSERT_ID();";

            using var userCmd = new MySqlCommand(insertUserSql, connection, transaction);
            userCmd.Parameters.AddWithValue("@username", uniqueUsername);
            userCmd.Parameters.AddWithValue("@email", info.Email);
            userCmd.Parameters.AddWithValue("@avatarUrl", (object?)info.AvatarUrl ?? DBNull.Value);

            var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

            // 3. Insert link into user_auth_providers table
            const string insertProviderSql = @"
                INSERT INTO user_auth_providers (user_id, provider, provider_user_id)
                VALUES (@userId, 'twitch', @twitchUserId);";

            using var providerCmd = new MySqlCommand(insertProviderSql, connection, transaction);
            providerCmd.Parameters.AddWithValue("@userId", userId);
            providerCmd.Parameters.AddWithValue("@twitchUserId", info.TwitchUserId);
            await providerCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return new User
            {
                UserId = userId,
                Username = uniqueUsername,
                Email = info.Email,
                Role = "Viewer",
                EmailVerified = true,
                AvatarUrl = info.AvatarUrl
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task SaveLoginCodeAsync(string codeHash, int userId, DateTime expiresAt)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO external_login_codes (code_hash, user_id, expires_at, is_used)
            VALUES (@codeHash, @userId, @expiresAt, FALSE);";

        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@codeHash", codeHash);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@expiresAt", expiresAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int?> ConsumeLoginCodeAsync(string codeHash)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT user_id FROM external_login_codes
            WHERE code_hash = @codeHash AND is_used = FALSE AND expires_at > UTC_TIMESTAMP()
            LIMIT 1;
            UPDATE external_login_codes SET is_used = TRUE WHERE code_hash = @codeHash;";

        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@codeHash", codeHash);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader.GetInt32("user_id");
        }

        return null;
    }

    private static async Task<bool> UsernameExistsAsync(MySqlConnection conn, MySqlTransaction tx, string username)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE username = @username;";
        using var cmd = new MySqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@username", username);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }
}