using BattleEconomyService.DTOs;

namespace BattleEconomyService.Services;

/// <summary>
/// Client abstraction for querying StreamService over HTTP (SCRUM-118).
/// Decouples higher-level validators from HTTP transport, serialization, and resilience concerns.
/// </summary>
public interface IStreamServiceClient
{
    /// <summary>
    /// Queries StreamService for the current broadcast status of the specified stream.
    /// Fails safely (returns non-live result) on network, timeout, or HTTP errors.
    /// </summary>
    Task<StreamLivenessResult> GetStreamLivenessAsync(int streamId, CancellationToken cancellationToken = default);
}
