using System.Threading.Tasks;
using UserService.Entities;

namespace UserService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByUsernameExcludingUserAsync(string username, int userId);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdatePasswordAsync(int userId, string passwordHash);
    Task<User?> GetByEmailExcludingUserAsync(string email, int userId);
    Task<bool> UpdateProfileAsync(int userId, string username, string email, string? avatarUrl, string? displayName, string? bio, string? bannerUrl, bool emailVerified);
    Task<bool> DeleteUserAsync(int userId);
}
