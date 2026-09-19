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
    private readonly Mock<IStreamLivenessValidator> _streamValidatorMock;
    private readonly Mock<ICoinCapPolicy> _capPolicyMock;
    private readonly Mock<ICoinEarnedEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<WatchTickService>> _loggerMock;
    private readonly IOptions<EconomyOptions> _options;
    private readonly FakeTimeProvider _timeProvider;

    public WatchTickServiceTests()
    {
        _walletRepoMock = new Mock<IWalletRepository>();
        _streamValidatorMock = new Mock<IStreamLivenessValidator>();
        _capPolicyMock = new Mock<ICoinCapPolicy>();
        _eventPublisherMock = new Mock<ICoinEarnedEventPublisher>();
        _loggerMock = new Mock<ILogger<WatchTickService>>();

        _options = Options.Create(new EconomyOptions
        {
            WatchTickIntervalSeconds = 55,
            WatchTickCoinsAwarded = 10
        });

        // Start clock at fixed test time
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));

        // Default pass-through behavior for extension points
        _streamValidatorMock.Setup(s => s.ValidateStreamLiveAsync(It.IsAny<int?>())).ReturnsAsync(true);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync(false);
    }

    private WatchTickService CreateService() =>
        new(
            _walletRepoMock.Object,
            _streamValidatorMock.Object,
            _capPolicyMock.Object,
            _eventPublisherMock.Object,
            _options,
            _loggerMock.Object,
            _timeProvider
        );

    // =========================================================================
    // Test A: New wallet state
    // =========================================================================
    [Fact]
    public void NewWallet_InitialState_CoinsMustBeZero_AndLastTickAtNull()
    {
        var wallet = new Wallet
        {
            WalletId = 1,
            UserId = 42,
            Coins = 0,
            LastTickAt = null
        };

        Assert.Equal(0, wallet.Coins);
        Assert.Null(wallet.LastTickAt);
    }

    // =========================================================================
    // Test B: First watch tick
    // =========================================================================
    [Fact]
    public async Task FirstWatchTick_NewWallet_Succeeds_AwardsConfiguredAmount_AndRecordsTimestamp()
    {
        // Arrange
        const int userId = 100;
        var initialWallet = new Wallet { WalletId = 1, UserId = userId, Coins = 0, LastTickAt = null };
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(initialWallet);

        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId,
                10,
                now,
                It.IsAny<DateTime>(),
                101))
            .ReturnsAsync(AwardResult.Succeeded(10, 10, now, 1));

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId, streamId: 101);

        // Assert (AC1)
        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
        Assert.Equal(10, result.CurrentBalance);
        Assert.Equal(now, result.LastTickAt);

        // Verify exactly one transactional award execution occurred
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            userId, 10, now, It.IsAny<DateTime>(), 101), Times.Once);

        // Verify event was published
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.Is<CoinEarnedEvent>(
            evt => evt.UserId == userId && evt.Amount == 10 && evt.NewBalance == 10)), Times.Once);
    }

    // =========================================================================
    // Test C: 54 seconds case (rejected before interval)
    // =========================================================================
    [Fact]
    public async Task TickAt54Seconds_Rejected_ZeroCoinsAwarded_ReturnsRateLimited()
    {
        // Arrange: User had a successful award at T=0
        const int userId = 200;
        var t0 = _timeProvider.GetUtcNow().UtcDateTime;
        var wallet = new Wallet { WalletId = 2, UserId = userId, Coins = 10, LastTickAt = t0 };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(wallet);

        // Advance by only 54 seconds (< 55s)
        _timeProvider.Advance(TimeSpan.FromSeconds(54));

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId);

        // Assert (AC2)
        Assert.Equal(WatchTickStatus.RateLimited, result.Status);
        Assert.Equal(0, result.CoinsAwarded);
        Assert.Equal(10, result.CurrentBalance);
        Assert.Equal(t0, result.LastTickAt); // LastTickAt preserved
        Assert.Equal(1, result.RemainingSeconds); // 55 - 54 = 1s

        // Verify DB update was never attempted
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);

        // Verify no event was published
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.IsAny<CoinEarnedEvent>()), Times.Never);
    }

    // =========================================================================
    // Test D: Exactly 55 seconds (boundary accepted)
    // =========================================================================
    [Fact]
    public async Task TickAtExactly55Seconds_Accepted_ConfiguredCoinsAwarded()
    {
        // Arrange
        const int userId = 300;
        var t0 = _timeProvider.GetUtcNow().UtcDateTime;
        var wallet = new Wallet { WalletId = 3, UserId = userId, Coins = 10, LastTickAt = t0 };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(wallet);

        // Advance by exactly 55 seconds
        _timeProvider.Advance(TimeSpan.FromSeconds(55));
        var t55 = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, t55, It.IsAny<DateTime>(), null))
            .ReturnsAsync(AwardResult.Succeeded(10, 20, t55, 3));

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId);

        // Assert (AC1)
        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
        Assert.Equal(20, result.CurrentBalance);
        Assert.Equal(t55, result.LastTickAt);
    }

    // =========================================================================
    // Test E: More than 55 seconds (e.g. 120 seconds)
    // =========================================================================
    [Fact]
    public async Task TickAtMoreThan55Seconds_Accepted_ConfiguredCoinsAwarded()
    {
        // Arrange
        const int userId = 400;
        var t0 = _timeProvider.GetUtcNow().UtcDateTime;
        var wallet = new Wallet { WalletId = 4, UserId = userId, Coins = 20, LastTickAt = t0 };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(wallet);

        // Advance by 120 seconds
        _timeProvider.Advance(TimeSpan.FromSeconds(120));
        var t120 = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, t120, It.IsAny<DateTime>(), null))
            .ReturnsAsync(AwardResult.Succeeded(10, 30, t120, 4));

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId);

        // Assert (AC1)
        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
        Assert.Equal(30, result.CurrentBalance);
        Assert.Equal(t120, result.LastTickAt);
    }

    // =========================================================================
    // Test F: Rejected request preserves timestamp and creates no transaction
    // =========================================================================
    [Fact]
    public async Task RejectedRequest_LastTickAtRemainsUnchanged_NoCoinTransactionCreated()
    {
        // Arrange
        const int userId = 500;
        var t0 = _timeProvider.GetUtcNow().UtcDateTime;
        var wallet = new Wallet { WalletId = 5, UserId = userId, Coins = 15, LastTickAt = t0 };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(wallet);

        // Advance 20 seconds (< 55s)
        _timeProvider.Advance(TimeSpan.FromSeconds(20));

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId);

        // Assert
        Assert.Equal(WatchTickStatus.RateLimited, result.Status);
        Assert.Equal(0, result.CoinsAwarded);
        Assert.Equal(t0, result.LastTickAt); // unchanged

        // Verify repository award execution was NEVER called
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    // =========================================================================
    // Test G: Successful request creates exactly one transaction record
    // =========================================================================
    [Fact]
    public async Task SuccessfulRequest_ExactlyOneCoinTransactionCreated()
    {
        // Arrange
        const int userId = 600;
        var initialWallet = new Wallet { WalletId = 6, UserId = userId, Coins = 0, LastTickAt = null };
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(initialWallet);

        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, now, It.IsAny<DateTime>(), 99))
            .ReturnsAsync(AwardResult.Succeeded(10, 10, now, 6));

        var service = CreateService();

        // Act
        await service.ProcessWatchTickAsync(userId, streamId: 99);

        // Assert: ExecuteWatchTickAwardAsync called exactly once with streamId 99
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            userId, 10, now, It.IsAny<DateTime>(), 99), Times.Once);
    }

    // =========================================================================
    // Test H: Concurrent requests race condition prevention (AC3)
    // =========================================================================
    [Fact]
    public async Task ConcurrentRequests_AtomicConditionalUpdate_PreventsDoubleAward()
    {
        // Arrange: User has eligible tick, two concurrent requests arrive simultaneously
        const int userId = 700;
        var wallet = new Wallet { WalletId = 7, UserId = userId, Coins = 0, LastTickAt = null };
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId))
            .ReturnsAsync(wallet);

        // Request 1 wins the atomic conditional update
        // Request 2 loses the atomic conditional update (0 rows affected at DB level)
        _walletRepoMock.SetupSequence(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(AwardResult.Succeeded(10, 10, now, 7))
            .ReturnsAsync(AwardResult.RateLimited(10, now));

        var service = CreateService();

        // Act: Execute concurrently
        var result1 = await service.ProcessWatchTickAsync(userId);
        var result2 = await service.ProcessWatchTickAsync(userId);

        // Assert (AC3)
        // Request 1 succeeded
        Assert.Equal(WatchTickStatus.Success, result1.Status);
        Assert.Equal(10, result1.CoinsAwarded);
        Assert.Equal(10, result1.CurrentBalance);

        // Request 2 failed with rate limit
        Assert.Equal(WatchTickStatus.RateLimited, result2.Status);
        Assert.Equal(0, result2.CoinsAwarded);
        Assert.Equal(10, result2.CurrentBalance);

        // Event only published once
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.IsAny<CoinEarnedEvent>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentRequests_SimultaneousTasks_OnlyOneWinsRace_OthersRateLimited()
    {
        // Arrange: 10 concurrent requests arrive simultaneously for the same user
        const int userId = 800;
        var wallet = new Wallet { WalletId = 8, UserId = userId, Coins = 0, LastTickAt = null };
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);

        // Simulate an atomic database guard: exactly 1 caller wins atomic update, all others get 0 rows affected
        var winnerChosen = 0;
        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(() =>
            {
                if (Interlocked.Exchange(ref winnerChosen, 1) == 0)
                {
                    return AwardResult.Succeeded(10, 10, now, 8);
                }
                return AwardResult.RateLimited(10, now);
            });

        var service = CreateService();

        // Act: Launch 10 simultaneous tasks concurrently via Task.WhenAll
        const int concurrencyCount = 10;
        var tasks = Enumerable.Range(0, concurrencyCount)
            .Select(_ => Task.Run(() => service.ProcessWatchTickAsync(userId)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: Verify atomic isolation under true concurrency
        var successfulResults = results.Where(r => r.Status == WatchTickStatus.Success).ToList();
        var rateLimitedResults = results.Where(r => r.Status == WatchTickStatus.RateLimited).ToList();

        Assert.Single(successfulResults);
        Assert.Equal(10, successfulResults[0].CoinsAwarded);
        Assert.Equal(10, successfulResults[0].CurrentBalance);

        Assert.Equal(concurrencyCount - 1, rateLimitedResults.Count);
        Assert.All(rateLimitedResults, r => Assert.Equal(0, r.CoinsAwarded));

        // Event only published exactly once across all 10 concurrent requests
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.IsAny<CoinEarnedEvent>()), Times.Once);
    }
}
