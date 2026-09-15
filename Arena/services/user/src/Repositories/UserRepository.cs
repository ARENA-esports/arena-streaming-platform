using System.Data;
using System.Threading.Tasks;
using Dapper;
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

    private IDbConnection CreateConnection() => new MySqlConnection(ConnectionString);

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM users WHERE user_id = @UserId LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM users WHERE email = @Email LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM users WHERE username = @Username LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByEmailExcludingUserAsync(string email, int userId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM users WHERE email = @Email AND user_id != @UserId LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email, UserId = userId });
    }

    public async Task<User?> GetByUsernameExcludingUserAsync(string username, int userId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM users WHERE username = @Username AND user_id != @UserId LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username, UserId = userId });
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO users (username, email, password_hash, role)
            VALUES (@Username, @Email, @PasswordHash, @Role);
            SELECT LAST_INSERT_ID();
        ";
        
        var id = await connection.ExecuteScalarAsync<ulong>(sql, new
        {
            Username = user.Username,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role
        });
        
        return (int)id;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE users SET password_hash = @PasswordHash WHERE user_id = @UserId";
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            PasswordHash = passwordHash
        });

        return rowsAffected > 0;
    }

    // Added missing parameters: displayName, bio, bannerUrl
    public async Task<bool> UpdateProfileAsync(int userId, string username, string email, string? avatarUrl, string? displayName, string? bio, string? bannerUrl, bool emailVerified)
    {
        using var connection = CreateConnection();
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

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            Username = username,
            Email = email,
            AvatarUrl = avatarUrl,
            DisplayName = displayName,
            Bio = bio,
            BannerUrl = bannerUrl,
            EmailVerified = emailVerified
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "DELETE FROM users WHERE user_id = @UserId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }

    public async Task<bool> VerifyEmailAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE users SET email_verified = true WHERE user_id = @UserId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "DELETE FROM users WHERE user_id = @UserId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }
}
