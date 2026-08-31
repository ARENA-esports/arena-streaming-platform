using System.Threading.Tasks;
using UserService.Entities;

namespace UserService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdatePasswordAsync(int userId, string passwordHash);
}
