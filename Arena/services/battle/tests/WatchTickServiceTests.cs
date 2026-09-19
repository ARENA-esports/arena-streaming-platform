using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using BattleEconomyService.Configuration;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using BattleEconomyService.Services;

namespace BattleEconomyService.Tests;

public class WatchTickServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<ICoinTransactionRepository> _txRepoMock;
    private readonly Mock<IStreamLivenessValidator> _streamValidatorMock;
    private readonly Mock<ICoinCapPolicy> _capPolicyMock;
    private readonly Mock<ICoinEarnedEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<WatchTickService>> _loggerMock;
    private readonly IOptions<EconomyOptions> _options;
    private readonly FakeTimeProvider _timeProvider;

    public WatchTickServiceTests()
    {
        _walletRepoMock = new Mock<IWalletRepository>();
        _txRepoMock = new Mock<ICoinTransactionRepository>();
        _streamValidatorMock = new Mock<IStreamLivenessValidator>();
        _capPolicyMock = new Mock<ICoinCapPolicy>();
        _eventPublisherMock = new Mock<ICoinEarnedEventPublisher>();
        _loggerMock = new Mock<ILogger<WatchTickService>>();

        _options = Options.Create(new EconomyOptions
        {
            WatchTickIntervalSeconds = 55,
            WatchTickCoinsAwarded = 10
        });

        // Start fake clock at 2026-09-20 12:00:00 UTC
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));

        // Default extension point mocks (pass-through for SCRUM-114)
        _streamValidatorMock.Setup(s => s.ValidateStreamLiveAsync(It.IsAny<int?>())).ReturnsAsync(true);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync(false);
    }

    private WatchTickService CreateService() =>
        new(
            _walletRepoMock.Object,
            _txRepoMock.Object,
            _streamValidatorMock.Object,
            _capPolicyMock.Object,
            _eventPublisherMock.Object,
            _options,
            _loggerMock.Object,
            _timeProvider
        );

    [Fact]
    public void InitialWallet_MustHaveZeroCoins()
    {
        // AC: A newly created wallet MUST have coins = 0
        var newWallet = new Wallet
        {
            WalletId = 1,
            UserId = 42,
            Coins = 0,
            LastTickAt = null
        };

        Assert.Equal(0, newWallet.Coins);
        Assert.Null(newWallet.LastTickAt);
    }

    [Fact]
    public async Task FirstWatchTick_NewWallet_AwardsConfiguredCoins_AndRecordsTimestamp()
    {
        // Arrange
        const int userId = 100;
        var initialWallet = new Wallet { WalletId = 1, UserId = userId, Coins = 0, LastTickAt = null };
        var updatedWallet = new Wallet { WalletId = 1, UserId = userId, Coins = 10, LastTickAt = _timeProvider.GetUtcNow().UtcDateTime };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(initialWallet);

        _walletRepoMock.Setup(r => r.TryAwardWatchTickAsync(
                userId,
                10,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(updatedWallet);

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId, streamId: 101);

        // Assert
        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
        Assert.Equal(10, result.CurrentBalance);
        Assert.NotNull(result.LastTickAt);

        // Verify transaction recorded
        _txRepoMock.Verify(t => t.RecordTransactionAsync(userId, 1, 10, "WATCH_TICK", 101), Times.Once);

        // Verify event published
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.Is<CoinEarnedEvent>(
            evt => evt.UserId == userId && evt.Amount == 10 && evt.NewBalance == 10)), Times.Once);
    }

    [Fact]
    public async Task ValidTick_After55Seconds_AwardsCoins_AndUpdatesTimestamp()
    {
        // Arrange: User earned at T=0
        const int userId = 200;
        var initialTime = _timeProvider.GetUtcNow().UtcDateTime;
        var existingWallet = new Wallet { WalletId = 2, UserId = userId, Coins = 10, LastTickAt = initialTime };

        // Advance time by 55 seconds
        _timeProvider.Advance(TimeSpan.FromSeconds(55));
        var newTime = _timeProvider.GetUtcNow().UtcDateTime;

        var updatedWallet = new Wallet { WalletId = 2, UserId = userId, Coins = 20, LastTickAt = newTime };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(existingWallet);

        _walletRepoMock.Setup(r => r.TryAwardWatchTickAsync(
                userId,
                10,
                newTime,
                It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(updatedWallet);

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId);

        // Assert (AC1)
        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
        Assert.Equal(20, result.CurrentBalance);
        Assert.Equal(newTime, result.LastTickAt);
    }

    [Fact]
    public async Task InvalidTick_Before55Seconds_ReturnsRateLimited_ZeroCoinsAwarded()
    {
        // Arrange: User earned at T=0
        const int userId = 300;
        var initialTime = _timeProvider.GetUtcNow().UtcDateTime;
        var existingWallet = new Wallet { WalletId = 3, UserId = userId, Coins = 10, LastTickAt = initialTime };

        // Advance time by only 30 seconds (< 55s)
        _timeProvider.Advance(TimeSpan.FromSeconds(30));

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(existingWallet);

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId);

        // Assert (AC2)
        Assert.Equal(WatchTickStatus.RateLimited, result.Status);
        Assert.Equal(0, result.CoinsAwarded);
        Assert.Equal(10, result.CurrentBalance);
        Assert.Equal(initialTime, result.LastTickAt); // previous timestamp untouched
        Assert.Equal(25, result.RemainingSeconds);   // 55 - 30 = 25s remaining

        // Verify DB update was NOT called
        _walletRepoMock.Verify(r => r.TryAwardWatchTickAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);

        // Verify no transaction recorded
        _txRepoMock.Verify(t => t.RecordTransactionAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task BoundaryTest_At54Seconds_Fails_At55Seconds_Succeeds()
    {
        // Arrange
        const int userId = 400;
        var t0 = _timeProvider.GetUtcNow().UtcDateTime;
        var wallet = new Wallet { WalletId = 4, UserId = userId, Coins = 10, LastTickAt = t0 };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);

        var service = CreateService();

        // Test at 54 seconds -> must fail
        _timeProvider.Advance(TimeSpan.FromSeconds(54));
        var result54 = await service.ProcessWatchTickAsync(userId);
        Assert.Equal(WatchTickStatus.RateLimited, result54.Status);
        Assert.Equal(0, result54.CoinsAwarded);
        Assert.Equal(1, result54.RemainingSeconds);

        // Advance 1 more second to reach exactly 55 seconds -> must succeed
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        var t55 = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.TryAwardWatchTickAsync(
                userId, 10, t55, It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new Wallet { WalletId = 4, UserId = userId, Coins = 20, LastTickAt = t55 });

        var result55 = await service.ProcessWatchTickAsync(userId);
        Assert.Equal(WatchTickStatus.Success, result55.Status);
        Assert.Equal(10, result55.CoinsAwarded);
        Assert.Equal(20, result55.CurrentBalance);
    }

    [Fact]
    public async Task ConcurrentRequests_AtomicConditionalUpdate_PreventsDoubleAward()
    {
        // Arrange: User has eligible tick, but two concurrent requests race to DB
        const int userId = 500;
        var wallet = new Wallet { WalletId = 5, UserId = userId, Coins = 0, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);

        // Request 1 succeeds (rowsAffected = 1)
        _walletRepoMock.SetupSequence(r => r.TryAwardWatchTickAsync(
                userId, 10, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true)   // Request 1 wins the race
            .ReturnsAsync(false);  // Request 2 loses the race (0 rows updated)

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new Wallet { WalletId = 5, UserId = userId, Coins = 10, LastTickAt = _timeProvider.GetUtcNow().UtcDateTime });

        var service = CreateService();

        // Act
        var result1 = await service.ProcessWatchTickAsync(userId);
        var result2 = await service.ProcessWatchTickAsync(userId);

        // Assert (AC3)
        // Request 1 succeeded
        Assert.Equal(WatchTickStatus.Success, result1.Status);
        Assert.Equal(10, result1.CoinsAwarded);

        // Request 2 was rejected atomically by the conditional update
        Assert.Equal(WatchTickStatus.RateLimited, result2.Status);
        Assert.Equal(0, result2.CoinsAwarded);

        // Only 1 transaction was recorded in ledger
        _txRepoMock.Verify(t => t.RecordTransactionAsync(
            userId, 5, 10, "WATCH_TICK", It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task StreamNotLive_ReturnsStreamNotLive_ZeroCoinsAwarded()
    {
        // Arrange
        const int userId = 600;
        var wallet = new Wallet { WalletId = 6, UserId = userId, Coins = 0, LastTickAt = null };
        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);

        // Simulate stream liveness check failing (extension point)
        _streamValidatorMock.Setup(s => s.ValidateStreamLiveAsync(999)).ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId, streamId: 999);

        // Assert
        Assert.Equal(WatchTickStatus.StreamNotLive, result.Status);
        Assert.Equal(0, result.CoinsAwarded);
        _walletRepoMock.Verify(r => r.TryAwardWatchTickAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }
}
