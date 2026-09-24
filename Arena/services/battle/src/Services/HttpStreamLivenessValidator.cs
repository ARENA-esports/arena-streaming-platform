using Microsoft.Extensions.Logging;

namespace BattleEconomyService.Services;

/// <summary>
/// HTTP-backed implementation of <see cref="IStreamLivenessValidator"/> (SCRUM-118).
/// Queries StreamService via <see cref="IStreamServiceClient"/> to verify stream liveness
/// before awarding viewer coins.
/// </summary>
public class HttpStreamLivenessValidator : IStreamLivenessValidator
{
    private readonly IStreamServiceClient _streamServiceClient;
    private readonly ILogger<HttpStreamLivenessValidator> _logger;

    public HttpStreamLivenessValidator(
        IStreamServiceClient streamServiceClient,
        ILogger<HttpStreamLivenessValidator> logger)
    {
        _streamServiceClient = streamServiceClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateStreamLiveAsync(int? streamId, CancellationToken cancellationToken = default)
    {
        // When no stream context is supplied, preserve the existing SCRUM-114 watch-tick contract
        // without introducing a breaking API change for optional streamId requests.
        if (streamId is null)
        {
            _logger.LogDebug("No stream ID specified; bypassing stream liveness validation.");
            return true;
        }

        // Invalid non-positive stream ID fails safe
        if (streamId <= 0)
        {
            _logger.LogWarning("Invalid stream ID {StreamId} provided to liveness validator; rejecting.", streamId);
            return false;
        }

        try
        {
            var result = await _streamServiceClient.GetStreamLivenessAsync(streamId.Value, cancellationToken);

            if (result.IsLive)
            {
                _logger.LogInformation("Stream {StreamId} validated as live.", streamId);
                return true;
            }

            _logger.LogInformation(
                "Stream {StreamId} rejected by liveness check (Status: {Status}, Error: {Error}).",
                streamId, result.Status, result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during stream liveness validation for stream {StreamId}; failing safe.", streamId);
            return false;
        }
    }
}
