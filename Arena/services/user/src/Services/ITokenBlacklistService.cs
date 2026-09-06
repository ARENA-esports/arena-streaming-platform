using System;
using System.Threading.Tasks;

namespace UserService.Services;

public interface ITokenBlacklistService
{
    Task RevokeTokenAsync(string jti, int? userId, DateTime expiresAt);
    Task<bool> IsTokenRevokedAsync(string jti);
    Task RevokeUserTokensAsync(int userId, DateTime expiresAt);
    Task<bool> IsUserTokenRevokedAsync(int userId, DateTime? tokenIssuedAt);
}
