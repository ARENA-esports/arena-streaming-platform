namespace BattleEconomyService.Services;

/// <summary>
/// Extension point for SCRUM-115: per-user / per-stream coin cap within a time window.
/// </summary>
public interface ICoinCapPolicy
{
    Task<bool> IsCapExceededAsync(int userId, int? streamId);
}

public class NoOpCoinCapPolicy : ICoinCapPolicy
{
    public Task<bool> IsCapExceededAsync(int userId, int? streamId) => Task.FromResult(false);
}
