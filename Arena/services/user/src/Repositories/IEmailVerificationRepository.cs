using UserService.Entities;

namespace UserService.Repositories;

public interface IEmailVerificationRepository
{
    Task<int> CreateTokenAsync(EmailVerificationToken token);
    Task<EmailVerificationToken?> GetByTokenAsync(string token);
    Task<bool> MarkAsUsedAsync(string token);
    Task<int> InvalidateUserTokensAsync(int userId);
}
