using System.Threading.Tasks;
using UserService.Entities;

namespace UserService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailExcludingUserAsync(string email, int userId);
    Task<User?> GetByUsernameExcludingUserAsync(string username, int userId);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdatePasswordAsync(int userId, string passwordHash);
    Task<bool> UpdateProfileAsync(int userId, string username, string email, string? avatarUrl, bool emailVerified);
    Task<bool> VerifyEmailAsync(int userId);
}
