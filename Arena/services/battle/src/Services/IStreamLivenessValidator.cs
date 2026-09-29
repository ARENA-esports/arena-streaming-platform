namespace BattleEconomyService.Services;

/// <summary>
/// Extension point for SCRUM-118: stream liveness validation before awarding coins.
/// Decouples WatchTickService from internal HTTP communication with StreamService.
/// </summary>
public interface IStreamLivenessValidator
{
    Task<bool> ValidateStreamLiveAsync(int? streamId, CancellationToken cancellationToken = default);
}

public class PassThroughStreamLivenessValidator : IStreamLivenessValidator
{
    public Task<bool> ValidateStreamLiveAsync(int? streamId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
