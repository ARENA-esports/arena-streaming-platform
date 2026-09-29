using Microsoft.Extensions.Options;
using BattleEconomyService.Configuration;
using BattleEconomyService.Repositories;

namespace BattleEconomyService.Services;

/// <summary>
/// SCRUM-115 implementation of <see cref="ICoinCapPolicy"/>.
/// Enforces a per-stream rolling-window coin ceiling:
/// a viewer may earn at most <see cref="EconomyOptions.CoinCapPerWindow"/> coins
/// from WATCH_TICK events within any <see cref="EconomyOptions.CoinCapWindowSeconds"/>-second
/// sliding window for a given (user_id, stream_id) pair.
/// </summary>
/// <remarks>
/// When streamId is null, the check is skipped and the policy returns false.
/// No global-per-user aggregation is performed.
/// This preserves existing SCRUM-114 behaviour for requests without a stream context.
/// The underlying query leverages the pre-existing composite index
/// idx_transactions_user_stream_created (user_id, stream_id, created_at).
/// </remarks>
public class SlidingWindowCoinCapPolicy : ICoinCapPolicy
{
    private readonly ICoinTransactionRepository _txRepository;
    private readonly EconomyOptions _options;
    private readonly TimeProvider _timeProvider;

    public SlidingWindowCoinCapPolicy(
        ICoinTransactionRepository txRepository,
        IOptions<EconomyOptions> options,
        TimeProvider timeProvider)
    {
        _txRepository = txRepository;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<bool> IsCapExceededAsync(int userId, int? streamId)
    {
        // Per design decision: when no stream context is supplied, do not apply the cap.
        if (streamId is null)
        {
            return false;
        }

        // Calculate the start of the current rolling window (UTC).
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddSeconds(-_options.CoinCapWindowSeconds);

        // Query the ledger for accumulated WATCH_TICK coins in this window.
        var windowSum = await _txRepository.GetWindowCoinSumAsync(userId, streamId, windowStart);

        // Cap is exceeded when the viewer has already earned the maximum for this window.
        return windowSum >= _options.CoinCapPerWindow;
    }
}
