using UserService.Entities;

namespace UserService.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    int ExpiryMinutes { get; }
}
