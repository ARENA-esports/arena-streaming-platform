using System.Threading.Tasks;
using UserService.Entities;

namespace UserService.Repositories;

public interface IPasswordResetRepository
{
    Task<int> CreateTokenAsync(PasswordResetToken resetToken);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task<bool> MarkAsUsedAsync(string token);
    Task<int> InvalidateUserTokensAsync(int userId);
}
