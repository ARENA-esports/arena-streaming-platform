using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UserService.Repositories;

namespace UserService.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ITokenBlacklistRepository _repository;
    private readonly ConcurrentDictionary<string, DateTime> _inMemoryCache;

    public TokenBlacklistService(ITokenBlacklistRepository repository)
    {
        _repository = repository;
        _inMemoryCache = new ConcurrentDictionary<string, DateTime>();
    }

    public async Task RevokeTokenAsync(string jti, int? userId, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        _inMemoryCache[jti] = expiresAt;
        await _repository.RevokeTokenAsync(jti, userId, expiresAt);
    }

    public async Task<bool> IsTokenRevokedAsync(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        if (_inMemoryCache.TryGetValue(jti, out var expiresAt))
        {
            if (expiresAt > DateTime.UtcNow)
            {
                return true;
            }

            _inMemoryCache.TryRemove(jti, out _);
        }

        var isRevoked = await _repository.IsTokenRevokedAsync(jti);
        if (isRevoked)
        {
            _inMemoryCache[jti] = DateTime.UtcNow.AddMinutes(120);
        }

        return isRevoked;
    }
}
