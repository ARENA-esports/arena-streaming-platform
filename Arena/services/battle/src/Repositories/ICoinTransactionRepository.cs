using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public interface ICoinTransactionRepository
{
    Task<long> RecordTransactionAsync(int userId, int walletId, int amount, string type, int? streamId);
    Task<IEnumerable<CoinTransaction>> GetRecentTransactionsAsync(int userId, int limit = 50);
}
