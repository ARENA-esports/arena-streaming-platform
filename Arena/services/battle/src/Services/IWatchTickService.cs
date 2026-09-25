using BattleEconomyService.DTOs;

namespace BattleEconomyService.Services;

public interface IWatchTickService
{
    Task<WatchTickResult> ProcessWatchTickAsync(int userId, int? streamId = null);
}
