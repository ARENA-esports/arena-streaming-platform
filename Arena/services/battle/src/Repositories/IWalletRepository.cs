using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet> GetOrCreateWalletAsync(int userId);
    Task<bool> TryAwardWatchTickAsync(int userId, int coins, DateTime currentTime, DateTime threshold);
    Task<AwardResult> ExecuteWatchTickAwardAsync(int userId, int coins, DateTime currentTime, DateTime threshold, int? streamId);
}
