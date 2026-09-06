using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UserService.Repositories;

namespace UserService.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ITokenBlacklistRepository _repository;
    private readonly ConcurrentDictionary<string, DateTime> _inMemoryCache;
    private readonly ConcurrentDictionary<int, DateTime> _userRevocationCache;

    public TokenBlacklistService(ITokenBlacklistRepository repository)
    {
        _repository = repository;
        _inMemoryCache = new ConcurrentDictionary<string, DateTime>();
        _userRevocationCache = new ConcurrentDictionary<int, DateTime>();
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

    public async Task RevokeUserTokensAsync(int userId, DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        _userRevocationCache[userId] = now;
        await _repository.RevokeUserTokensAsync(userId, expiresAt);
    }

    public async Task<bool> IsUserTokenRevokedAsync(int userId, DateTime? tokenIssuedAt)
    {
        if (_userRevocationCache.TryGetValue(userId, out var cachedRevokedAt))
        {
            if (tokenIssuedAt.HasValue && tokenIssuedAt.Value < cachedRevokedAt)
            {
                return true;
            }
        }

        var dbRevokedAt = await _repository.GetUserRevocationTimeAsync(userId);
        if (dbRevokedAt.HasValue)
        {
            _userRevocationCache[userId] = dbRevokedAt.Value;
            if (tokenIssuedAt.HasValue && tokenIssuedAt.Value < dbRevokedAt.Value)
            {
                return true;
            }
        }

        return false;
    }
}
