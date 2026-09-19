namespace BattleEconomyService.Services;

/// <summary>
/// Extension point for SCRUM-118: stream liveness validation before awarding coins.
/// </summary>
public interface IStreamLivenessValidator
{
    Task<bool> ValidateStreamLiveAsync(int? streamId);
}

public class PassThroughStreamLivenessValidator : IStreamLivenessValidator
{
    public Task<bool> ValidateStreamLiveAsync(int? streamId) => Task.FromResult(true);
}
