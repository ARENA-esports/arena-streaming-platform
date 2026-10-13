using UserService.Entities;
using UserService.Models;

namespace UserService.Repositories;

public interface IExternalAuthRepository
{
    Task<User?> GetUserByTwitchIdAsync(string twitchUserId);
    Task<User> CreateUserFromTwitchAsync(TwitchUserInfo info);
    Task SaveLoginCodeAsync(string codeHash, int userId, DateTime expiresAt);
    Task<int?> ConsumeLoginCodeAsync(string codeHash);
}