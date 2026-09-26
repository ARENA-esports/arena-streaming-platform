using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;

namespace BattleEconomyService.Services;

/// <summary>
/// Typed HTTP client implementation for communicating with StreamService (SCRUM-118).
/// Queries GET api/streams/{streamId}, deserializes minimal StreamLivenessResponse,
/// and fails safely on non-200, 404, network errors, or timeouts.
/// </summary>
public class StreamServiceClient : IStreamServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StreamServiceClient> _logger;

    public StreamServiceClient(HttpClient httpClient, ILogger<StreamServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StreamLivenessResult> GetStreamLivenessAsync(int streamId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/streams/{streamId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("StreamService returned 404 Not Found for stream {StreamId}", streamId);
                return StreamLivenessResult.NotFound(streamId);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "StreamService returned HTTP status {StatusCode} for stream {StreamId}",
                    (int)response.StatusCode, streamId);
                return StreamLivenessResult.Failed(streamId, $"StreamService returned HTTP {(int)response.StatusCode}.");
            }

            var streamResponse = await response.Content.ReadFromJsonAsync<StreamLivenessResponse>(cancellationToken);

            if (streamResponse == null || string.IsNullOrWhiteSpace(streamResponse.Status))
            {
                _logger.LogWarning("StreamService returned empty or invalid payload for stream {StreamId}", streamId);
                return StreamLivenessResult.Failed(streamId, "Empty or invalid response from StreamService.");
            }

            if (StreamLiveStatus.IsLive(streamResponse.Status))
            {
                _logger.LogInformation("Stream {StreamId} confirmed live from StreamService.", streamId);
                return StreamLivenessResult.Live(streamId);
            }

            _logger.LogInformation(
                "Stream {StreamId} confirmed not live from StreamService (Status: {Status}).",
                streamId, streamResponse.Status);
            return StreamLivenessResult.NotLive(streamId, streamResponse.Status);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("StreamService request timed out for stream {StreamId}", streamId);
            return StreamLivenessResult.Failed(streamId, "StreamService request timed out.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("StreamService request was cancelled for stream {StreamId}", streamId);
            return StreamLivenessResult.Failed(streamId, "StreamService request was cancelled.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error querying StreamService for stream {StreamId}", streamId);
            return StreamLivenessResult.Failed(streamId, "StreamService network error.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying StreamService for stream {StreamId}", streamId);
            return StreamLivenessResult.Failed(streamId, "Unexpected error calling StreamService.");
        }
    }
}
