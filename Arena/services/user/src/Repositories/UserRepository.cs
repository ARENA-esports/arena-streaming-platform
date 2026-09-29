using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using UserService.Entities;

namespace UserService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IConfiguration _configuration;
    private string ConnectionString => _configuration.GetConnectionString("UserDb") 
        ?? throw new System.InvalidOperationException("Connection string UserDb not found.");

    public UserRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "SELECT * FROM users WHERE user_id = @UserId LIMIT 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "SELECT * FROM users WHERE email = @Email LIMIT 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "SELECT * FROM users WHERE username = @Username LIMIT 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", username);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetByEmailExcludingUserAsync(string email, int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "SELECT * FROM users WHERE email = @Email AND user_id != @UserId LIMIT 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@UserId", userId);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetByUsernameExcludingUserAsync(string username, int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "SELECT * FROM users WHERE username = @Username AND user_id != @UserId LIMIT 1";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@UserId", userId);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = @"
            INSERT INTO users (username, email, password_hash, role)
            VALUES (@Username, @Email, @PasswordHash, @Role);
            SELECT LAST_INSERT_ID();
        ";
        
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", user.Username);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@Role", user.Role);
        
        var id = await command.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "UPDATE users SET password_hash = @PasswordHash WHERE user_id = @UserId";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateProfileAsync(int userId, string username, string email, string? avatarUrl, string? displayName, string? bio, string? bannerUrl, bool emailVerified)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = @"
            UPDATE users 
            SET username = @Username, 
                email = @Email, 
                avatar_url = @AvatarUrl,
                display_name = @DisplayName,
                bio = @Bio,
                banner_url = @BannerUrl,
                email_verified = @EmailVerified,
                updated_at = UTC_TIMESTAMP()
            WHERE user_id = @UserId";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@AvatarUrl", (object?)avatarUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@DisplayName", (object?)displayName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Bio", (object?)bio ?? DBNull.Value);
        command.Parameters.AddWithValue("@BannerUrl", (object?)bannerUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@EmailVerified", emailVerified);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateRoleAsync(int userId, string role)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "UPDATE users SET role = @Role, updated_at = UTC_TIMESTAMP() WHERE user_id = @UserId";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Role", role);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "DELETE FROM users WHERE user_id = @UserId";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> VerifyEmailAsync(int userId)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = "UPDATE users SET email_verified = true WHERE user_id = @UserId";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private static User MapUser(MySqlDataReader reader)
    {
        var hasDisplayName = ColumnExists(reader, "display_name");
        var hasBio = ColumnExists(reader, "bio");
        var hasBannerUrl = ColumnExists(reader, "banner_url");

        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            Username = reader.GetString(reader.GetOrdinal("username")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("password_hash")) ? string.Empty : reader.GetString(reader.GetOrdinal("password_hash")),
            Role = reader.GetString(reader.GetOrdinal("role")),
            EmailVerified = reader.GetBoolean(reader.GetOrdinal("email_verified")),
            AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString(reader.GetOrdinal("avatar_url")),
            DisplayName = hasDisplayName && !reader.IsDBNull(reader.GetOrdinal("display_name")) ? reader.GetString(reader.GetOrdinal("display_name")) : null,
            Bio = hasBio && !reader.IsDBNull(reader.GetOrdinal("bio")) ? reader.GetString(reader.GetOrdinal("bio")) : null,
            BannerUrl = hasBannerUrl && !reader.IsDBNull(reader.GetOrdinal("banner_url")) ? reader.GetString(reader.GetOrdinal("banner_url")) : null,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static bool ColumnExists(MySqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
