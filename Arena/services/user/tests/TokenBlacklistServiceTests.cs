using System;
using System.Threading.Tasks;
using Moq;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.Tests;

public class TokenBlacklistServiceTests
{
    private readonly Mock<ITokenBlacklistRepository> _mockRepo;
    private readonly TokenBlacklistService _service;

    public TokenBlacklistServiceTests()
    {
        _mockRepo = new Mock<ITokenBlacklistRepository>();
        _service = new TokenBlacklistService(_mockRepo.Object);
    }

    [Fact]
    public async Task RevokeTokenAsync_CallsRepositoryAndCachesInMemory()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(2);

        // Act
        await _service.RevokeTokenAsync(jti, 1, expiresAt);

        // Assert
        _mockRepo.Verify(r => r.RevokeTokenAsync(jti, 1, expiresAt), Times.Once);

        // Should return true from memory without hitting repository again
        _mockRepo.Reset();
        var isRevoked = await _service.IsTokenRevokedAsync(jti);
        Assert.True(isRevoked);
        _mockRepo.Verify(r => r.IsTokenRevokedAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IsTokenRevokedAsync_WhenNotInCache_QueriesRepository()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        _mockRepo.Setup(r => r.IsTokenRevokedAsync(jti)).ReturnsAsync(true);

        // Act
        var isRevoked = await _service.IsTokenRevokedAsync(jti);

        // Assert
        Assert.True(isRevoked);
        _mockRepo.Verify(r => r.IsTokenRevokedAsync(jti), Times.Once);
    }

    [Fact]
    public async Task IsTokenRevokedAsync_WithNullOrEmptyJti_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(await _service.IsTokenRevokedAsync(string.Empty));
        Assert.False(await _service.IsTokenRevokedAsync("   "));
        _mockRepo.Verify(r => r.IsTokenRevokedAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RevokeUserTokensAsync_CallsRepositoryAndInvalidatesPriorIssuedTokens()
    {
        // Arrange
        int userId = 42;
        var tokenIssuedAt = DateTime.UtcNow.AddMinutes(-10);

        // Act
        await _service.RevokeUserTokensAsync(userId, DateTime.UtcNow.AddHours(2));

        // Assert
        _mockRepo.Verify(r => r.RevokeUserTokensAsync(userId, It.IsAny<DateTime>()), Times.Once);

        // Token issued prior to revocation should be considered revoked
        var isRevoked = await _service.IsUserTokenRevokedAsync(userId, tokenIssuedAt);
        Assert.True(isRevoked);

        // Token issued after revocation should NOT be revoked
        var isNewTokenRevoked = await _service.IsUserTokenRevokedAsync(userId, DateTime.UtcNow.AddMinutes(5));
        Assert.False(isNewTokenRevoked);
    }
}
