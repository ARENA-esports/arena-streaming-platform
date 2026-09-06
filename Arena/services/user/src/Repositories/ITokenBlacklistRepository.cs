using System;
using System.Threading.Tasks;

namespace UserService.Repositories;

public interface ITokenBlacklistRepository
{
    Task RevokeTokenAsync(string jti, int? userId, DateTime expiresAt);
    Task<bool> IsTokenRevokedAsync(string jti);
    Task RevokeUserTokensAsync(int userId, DateTime expiresAt);
    Task<DateTime?> GetUserRevocationTimeAsync(int userId);
}
