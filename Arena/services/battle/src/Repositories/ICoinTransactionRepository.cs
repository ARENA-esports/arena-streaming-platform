using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public interface ICoinTransactionRepository
{
    Task<long> RecordTransactionAsync(int userId, int walletId, int amount, string type, int? streamId);
    Task<IEnumerable<CoinTransaction>> GetRecentTransactionsAsync(int userId, int limit = 50);

    /// <summary>
    /// Returns the total WATCH_TICK coins earned by the user for the given stream
    /// within the rolling window that starts at <paramref name="windowStart"/> (UTC).
    /// Uses the existing composite index idx_transactions_user_stream_created.
    /// When <paramref name="streamId"/> is null the cap check is skipped by the policy;
    /// this method still handles null safely (returns 0).
    /// </summary>
    Task<int> GetWindowCoinSumAsync(int userId, int? streamId, DateTime windowStart);
}
