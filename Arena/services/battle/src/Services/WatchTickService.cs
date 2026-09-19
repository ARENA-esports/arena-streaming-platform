using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BattleEconomyService.Configuration;
using BattleEconomyService.DTOs;
using BattleEconomyService.Repositories;

namespace BattleEconomyService.Services;

public class WatchTickService : IWatchTickService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IStreamLivenessValidator _streamValidator;
    private readonly ICoinCapPolicy _capPolicy;
    private readonly ICoinEarnedEventPublisher _eventPublisher;
    private readonly EconomyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WatchTickService> _logger;

    public WatchTickService(
        IWalletRepository walletRepository,
        IStreamLivenessValidator streamValidator,
        ICoinCapPolicy capPolicy,
        ICoinEarnedEventPublisher eventPublisher,
        IOptions<EconomyOptions> options,
        ILogger<WatchTickService> logger,
        TimeProvider? timeProvider = null)
    {
        _walletRepository = walletRepository;
        _streamValidator = streamValidator;
        _capPolicy = capPolicy;
        _eventPublisher = eventPublisher;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<WatchTickResult> ProcessWatchTickAsync(int userId, int? streamId = null)
    {
        // 1. Ensure wallet exists (new wallet starts with coins = 0, last_tick_at = null)
        var wallet = await _walletRepository.GetOrCreateWalletAsync(userId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var interval = TimeSpan.FromSeconds(_options.WatchTickIntervalSeconds);
        var threshold = now - interval;

        // 2. Pre-check: If last_tick_at was recently recorded, reject early
        if (wallet.LastTickAt.HasValue)
        {
            var elapsed = now - wallet.LastTickAt.Value;
            if (elapsed < interval)
            {
                var remaining = (int)Math.Ceiling((interval - elapsed).TotalSeconds);
                _logger.LogInformation(
                    "User {UserId} watch tick rejected by anti-farm check: {Elapsed}s elapsed, requires {Required}s",
                    userId, elapsed.TotalSeconds, _options.WatchTickIntervalSeconds);

                return WatchTickResult.TooEarly(wallet.Coins, wallet.LastTickAt.Value, Math.Max(1, remaining));
            }
        }

        // 3. Extension point: Stream liveness check (SCRUM-118)
        var isLive = await _streamValidator.ValidateStreamLiveAsync(streamId);
        if (!isLive)
        {
            return new WatchTickResult
            {
                Status = WatchTickStatus.StreamNotLive,
                CoinsAwarded = 0,
                CurrentBalance = wallet.Coins,
                LastTickAt = wallet.LastTickAt,
                Message = "Stream is not currently live."
            };
        }

        // Extension point: Cap policy check (SCRUM-115)
        var isCapped = await _capPolicy.IsCapExceededAsync(userId, streamId);
        if (isCapped)
        {
            return new WatchTickResult
            {
                Status = WatchTickStatus.CapExceeded,
                CoinsAwarded = 0,
                CurrentBalance = wallet.Coins,
                LastTickAt = wallet.LastTickAt,
                Message = "Coin cap reached for this stream window."
            };
        }

        // 4. Atomic conditional update & ledger recording in a single database transaction (AC3)
        var awardResult = await _walletRepository.ExecuteWatchTickAwardAsync(
            userId,
            _options.WatchTickCoinsAwarded,
            now,
            threshold,
            streamId);

        if (!awardResult.Success)
        {
            // Another concurrent request updated last_tick_at, or interval condition was not met
            _logger.LogWarning("Concurrent anti-farm collision or interval not met for user {UserId}", userId);
            var remaining = _options.WatchTickIntervalSeconds;
            if (awardResult.LastTickAt.HasValue)
            {
                var elapsed = now - awardResult.LastTickAt.Value;
                if (elapsed < interval)
                {
                    remaining = (int)Math.Ceiling((interval - elapsed).TotalSeconds);
                }
            }
            return WatchTickResult.TooEarly(awardResult.CurrentBalance, awardResult.LastTickAt, Math.Max(1, remaining));
        }

        // 5. Extension point: Publish CoinEarned event (SCRUM-117)
        await _eventPublisher.PublishCoinEarnedAsync(new CoinEarnedEvent(
            userId,
            awardResult.WalletId,
            _options.WatchTickCoinsAwarded,
            awardResult.CurrentBalance,
            streamId,
            now));

        _logger.LogInformation(
            "Awarded {Coins} coins to user {UserId}. New balance: {Balance}",
            _options.WatchTickCoinsAwarded, userId, awardResult.CurrentBalance);

        return WatchTickResult.Awarded(_options.WatchTickCoinsAwarded, awardResult.CurrentBalance, now);
    }
}
