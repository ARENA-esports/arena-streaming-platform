using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using BattleEconomyService.Configuration;
using BattleEconomyService.Repositories;
using BattleEconomyService.Services;

namespace BattleEconomyService.Tests;

public class SlidingWindowCoinCapPolicyTests
{
    private readonly Mock<ICoinTransactionRepository> _txRepoMock;
    private readonly FakeTimeProvider _timeProvider;
    private const int UserId = 42;
    private const int StreamId = 101;

    public SlidingWindowCoinCapPolicyTests()
    {
        _txRepoMock = new Mock<ICoinTransactionRepository>();
        // Fixed test clock: 2026-09-20 12:00:00 UTC
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));
    }

    private SlidingWindowCoinCapPolicy CreatePolicy(int capPerWindow = 50, int windowSeconds = 300) =>
        new(
            _txRepoMock.Object,
            Options.Create(new EconomyOptions
            {
                WatchTickIntervalSeconds = 55,
                WatchTickCoinsAwarded = 10,
                CoinCapWindowSeconds = windowSeconds,
                CoinCapPerWindow = capPerWindow
            }),
            _timeProvider
        );

    // =========================================================================
    // Helper: expected window start given the current frozen clock
    // =========================================================================
    private DateTime ExpectedWindowStart(int windowSeconds = 300) =>
        _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-windowSeconds);

    // =========================================================================
    // Test 1: Window sum below cap → false
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_SumBelowCap_ReturnsFalse()
    {
        // 40 coins earned, cap is 50
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .ReturnsAsync(40);

        var policy = CreatePolicy(capPerWindow: 50);
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.False(result);
    }

    // =========================================================================
    // Test 2: Window sum exactly equal to cap → true (boundary inclusive)
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_SumExactlyEqualToCap_ReturnsTrue()
    {
        // Exactly 50 coins earned, cap is 50
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .ReturnsAsync(50);

        var policy = CreatePolicy(capPerWindow: 50);
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.True(result);
    }

    // =========================================================================
    // Test 3: Window sum above cap → true
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_SumAboveCap_ReturnsTrue()
    {
        // 60 coins somehow accumulated (e.g., race with multiple in-flight requests)
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .ReturnsAsync(60);

        var policy = CreatePolicy(capPerWindow: 50);
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.True(result);
    }

    // =========================================================================
    // Test 4: Window boundary — exactly at 300-second mark uses correct windowStart
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_PassesCorrectWindowStartToRepository()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expectedWindowStart = now.AddSeconds(-300);

        DateTime capturedWindowStart = default;
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .Callback<int, int?, DateTime>((_, _, ws) => capturedWindowStart = ws)
            .ReturnsAsync(0);

        var policy = CreatePolicy(windowSeconds: 300);
        await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.Equal(expectedWindowStart, capturedWindowStart);
    }

    // =========================================================================
    // Test 5: Still inside window — cap applies from windowStart forward
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_StillInsideWindow_CapEnforced()
    {
        // Advance clock by 150 seconds (halfway through the 300s window)
        _timeProvider.Advance(TimeSpan.FromSeconds(150));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expectedWindowStart = now.AddSeconds(-300);

        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, expectedWindowStart))
            .ReturnsAsync(50); // cap reached

        var policy = CreatePolicy(windowSeconds: 300, capPerWindow: 50);
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.True(result);
    }

    // =========================================================================
    // Test 6: After window expires — old transactions fall outside new windowStart
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_AfterWindowExpires_ReturnsFalse()
    {
        // Advance clock by 301 seconds — the previous window is now outside the range
        _timeProvider.Advance(TimeSpan.FromSeconds(301));

        // Repository returns 0 because the new windowStart excludes old transactions
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        var policy = CreatePolicy(capPerWindow: 50);
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.False(result);
        // Verify the windowStart sent to the repository is after the old transactions
        _txRepoMock.Verify(r => r.GetWindowCoinSumAsync(
            UserId, StreamId,
            It.Is<DateTime>(ws => ws > new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc))),
            Times.Once);
    }

    // =========================================================================
    // Test 7 (AC3): Stream A cap does not affect Stream B
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_StreamACapDoesNotAffectStreamB()
    {
        const int streamA = 101;
        const int streamB = 202;

        // Stream A is at cap
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, streamA, It.IsAny<DateTime>()))
            .ReturnsAsync(50);
        // Stream B has earned nothing
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, streamB, It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        var policy = CreatePolicy(capPerWindow: 50);

        var capOnA = await policy.IsCapExceededAsync(UserId, streamA);
        var capOnB = await policy.IsCapExceededAsync(UserId, streamB);

        Assert.True(capOnA, "Stream A should be capped");
        Assert.False(capOnB, "Stream B should NOT be capped");
    }

    // =========================================================================
    // Test 8: Null streamId — always returns false (no global per-user cap)
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_NullStreamId_ReturnsFalseWithoutQueryingRepository()
    {
        var policy = CreatePolicy();
        var result = await policy.IsCapExceededAsync(UserId, streamId: null);

        Assert.False(result);
        // Repository must NOT be called when streamId is null
        _txRepoMock.Verify(r => r.GetWindowCoinSumAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime>()), Times.Never);
    }

    // =========================================================================
    // Test 9: Zero sum (no transactions yet) → false
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_NoTransactionsInWindow_ReturnsFalse()
    {
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        var policy = CreatePolicy();
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.False(result);
    }

    // =========================================================================
    // Test 10: One coin below cap → false (boundary below)
    // =========================================================================
    [Fact]
    public async Task IsCapExceededAsync_SumOneBeforeCap_ReturnsFalse()
    {
        _txRepoMock.Setup(r => r.GetWindowCoinSumAsync(UserId, StreamId, It.IsAny<DateTime>()))
            .ReturnsAsync(49); // cap is 50

        var policy = CreatePolicy(capPerWindow: 50);
        var result = await policy.IsCapExceededAsync(UserId, StreamId);

        Assert.False(result);
    }
}
