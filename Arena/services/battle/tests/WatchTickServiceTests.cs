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

    // =========================================================================
    // SCRUM-115 Tests: Sliding-window coin cap (ICoinCapPolicy)
    // =========================================================================

    // Test J: Cap exceeded → service returns CapExceeded status
    [Fact]
    public async Task CapExceeded_PolicyReturnsTrue_ServiceReturnsCapExceededStatus()
    {
        // Arrange
        const int userId = 1001;
        const int streamId = 101;
        var wallet = new Wallet { WalletId = 10, UserId = userId, Coins = 50, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.ProcessWatchTickAsync(userId, streamId);

        // Assert (AC1)
        Assert.Equal(WatchTickStatus.CapExceeded, result.Status);
    }

    // Test K: Cap exceeded → zero coins awarded
    [Fact]
    public async Task CapExceeded_PolicyReturnsTrue_ZeroCoinsAwarded()
    {
        const int userId = 1002;
        const int streamId = 101;
        var wallet = new Wallet { WalletId = 11, UserId = userId, Coins = 50, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(true);

        var service = CreateService();
        var result = await service.ProcessWatchTickAsync(userId, streamId);

        Assert.Equal(0, result.CoinsAwarded);
    }

    // Test L: Cap exceeded → wallet balance unchanged (no award DB call)
    [Fact]
    public async Task CapExceeded_PolicyReturnsTrue_WalletBalanceUnchanged()
    {
        const int userId = 1003;
        const int streamId = 101;
        const int originalBalance = 50;
        var wallet = new Wallet { WalletId = 12, UserId = userId, Coins = originalBalance, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(true);

        var service = CreateService();
        var result = await service.ProcessWatchTickAsync(userId, streamId);

        // Balance reported in response equals original (unchanged) balance
        Assert.Equal(originalBalance, result.CurrentBalance);

        // Verify no atomic DB award was attempted
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    // Test M: Cap exceeded → no coin transaction inserted
    [Fact]
    public async Task CapExceeded_PolicyReturnsTrue_NoCoinTransactionInserted()
    {
        const int userId = 1004;
        const int streamId = 101;
        var wallet = new Wallet { WalletId = 13, UserId = userId, Coins = 50, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(true);

        var service = CreateService();
        await service.ProcessWatchTickAsync(userId, streamId);

        // ExecuteWatchTickAwardAsync (which inserts the ledger record) must never be called
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);

        // No CoinEarned event published
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.IsAny<CoinEarnedEvent>()), Times.Never);
    }

    // Test N: Cap not exceeded → normal award proceeds
    [Fact]
    public async Task CapNotExceeded_PolicyReturnsFalse_NormalAwardProceeds()
    {
        const int userId = 1005;
        const int streamId = 101;
        var wallet = new Wallet { WalletId = 14, UserId = userId, Coins = 40, LastTickAt = null };
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(false);
        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, now, It.IsAny<DateTime>(), streamId))
            .ReturnsAsync(AwardResult.Succeeded(10, 50, now, 14));

        var service = CreateService();
        var result = await service.ProcessWatchTickAsync(userId, streamId);

        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
        Assert.Equal(50, result.CurrentBalance);
    }

    // Test O: Window rollover — cap policy returns false after window expires → earning allowed (AC2)
    [Fact]
    public async Task WindowRollover_AfterWindowExpires_CapPolicyReturnsFalse_EarningAllowed()
    {
        // Arrange: cap policy returns false (window has rolled over)
        const int userId = 1006;
        const int streamId = 101;
        var wallet = new Wallet { WalletId = 15, UserId = userId, Coins = 50, LastTickAt = null };

        // Advance clock past the 5-minute window
        _timeProvider.Advance(TimeSpan.FromSeconds(301));
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        // Policy returns false after rollover (new window has zero earned)
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(false);
        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, now, It.IsAny<DateTime>(), streamId))
            .ReturnsAsync(AwardResult.Succeeded(10, 60, now, 15));

        var service = CreateService();
        var result = await service.ProcessWatchTickAsync(userId, streamId);

        Assert.Equal(WatchTickStatus.Success, result.Status);
        Assert.Equal(10, result.CoinsAwarded);
    }

    // Test P (AC3): Cap on Stream A does not affect Stream B evaluation
    [Fact]
    public async Task CapOnStreamA_DoesNotAffectStreamB()
    {
        const int userId = 1007;
        const int streamA = 101;
        const int streamB = 202;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var wallet = new Wallet { WalletId = 16, UserId = userId, Coins = 50, LastTickAt = null };
        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);

        // Stream A is capped
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamA)).ReturnsAsync(true);
        // Stream B is not capped
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamB)).ReturnsAsync(false);

        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, now, It.IsAny<DateTime>(), streamB))
            .ReturnsAsync(AwardResult.Succeeded(10, 60, now, 16));

        var service = CreateService();

        // Act
        var resultA = await service.ProcessWatchTickAsync(userId, streamA);
        var resultB = await service.ProcessWatchTickAsync(userId, streamB);

        // Assert
        Assert.Equal(WatchTickStatus.CapExceeded, resultA.Status);
        Assert.Equal(0, resultA.CoinsAwarded);

        Assert.Equal(WatchTickStatus.Success, resultB.Status);
        Assert.Equal(10, resultB.CoinsAwarded);
    }

    // Test Q: Concurrency / flooding when cap is reached → all concurrent requests rejected with CapExceeded
    [Fact]
    public async Task ConcurrentRequests_WhenCapAlreadyReached_AllConcurrentRequestsRejected()
    {
        const int userId = 1008;
        const int streamId = 101;
        var wallet = new Wallet { WalletId = 17, UserId = userId, Coins = 50, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(true);

        var service = CreateService();

        const int concurrencyCount = 10;
        var tasks = Enumerable.Range(0, concurrencyCount)
            .Select(_ => Task.Run(() => service.ProcessWatchTickAsync(userId, streamId)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // All 10 requests must return CapExceeded with 0 coins
        Assert.All(results, r =>
        {
            Assert.Equal(WatchTickStatus.CapExceeded, r.Status);
            Assert.Equal(0, r.CoinsAwarded);
            Assert.Equal(50, r.CurrentBalance);
        });

        // Award method must never be called
        _walletRepoMock.Verify(r => r.ExecuteWatchTickAwardAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);

        // No event emitted
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.IsAny<CoinEarnedEvent>()), Times.Never);
    }

    // Test R: Concurrency / flooding near cap limit (at 40 coins) → only one can win, cap is not breached
    [Fact]
    public async Task ConcurrentRequests_NearCapLimit_OnlyOneCanWin_CapNotBreached()
    {
        const int userId = 1009;
        const int streamId = 101;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var wallet = new Wallet { WalletId = 18, UserId = userId, Coins = 40, LastTickAt = null };

        _walletRepoMock.Setup(r => r.GetOrCreateWalletAsync(userId)).ReturnsAsync(wallet);
        _capPolicyMock.Setup(c => c.IsCapExceededAsync(userId, streamId)).ReturnsAsync(false);

        var winnerChosen = 0;
        _walletRepoMock.Setup(r => r.ExecuteWatchTickAwardAsync(
                userId, 10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), streamId))
            .ReturnsAsync(() =>
            {
                if (Interlocked.Exchange(ref winnerChosen, 1) == 0)
                {
                    return AwardResult.Succeeded(10, 50, now, 18);
                }
                return AwardResult.RateLimited(50, now);
            });

        var service = CreateService();

        const int concurrencyCount = 10;
        var tasks = Enumerable.Range(0, concurrencyCount)
            .Select(_ => Task.Run(() => service.ProcessWatchTickAsync(userId, streamId)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successfulResults = results.Where(r => r.Status == WatchTickStatus.Success).ToList();
        var rateLimitedResults = results.Where(r => r.Status == WatchTickStatus.RateLimited).ToList();

        // Exactly one request succeeded and brought the balance to 50 (cap)
        Assert.Single(successfulResults);
        Assert.Equal(10, successfulResults[0].CoinsAwarded);
        Assert.Equal(50, successfulResults[0].CurrentBalance);

        // Remaining 9 requests were rate-limited with zero coins awarded
        Assert.Equal(concurrencyCount - 1, rateLimitedResults.Count);
        Assert.All(rateLimitedResults, r => Assert.Equal(0, r.CoinsAwarded));

        // Total coins awarded across all 10 concurrent requests cannot exceed 10
        var totalCoinsAwarded = results.Sum(r => r.CoinsAwarded);
        Assert.Equal(10, totalCoinsAwarded);

        // Exactly one CoinEarned event published
        _eventPublisherMock.Verify(e => e.PublishCoinEarnedAsync(It.IsAny<CoinEarnedEvent>()), Times.Once);
    }
}
