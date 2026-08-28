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
}
