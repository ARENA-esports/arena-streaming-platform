using Microsoft.Extensions.Logging;
using StreamService.Models;
using StreamService.Repositories;

namespace StreamService.Services;

public class StreamStatusService : IStreamStatusService
{
    private readonly IStreamRepository _streamRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly ILogger<StreamStatusService> _logger;

    public StreamStatusService(
        IStreamRepository streamRepository,
        IMatchRepository matchRepository,
        ILogger<StreamStatusService> logger)
    {
        _streamRepository = streamRepository;
        _matchRepository = matchRepository;
        _logger = logger;
    }

    public async Task<int?> ProcessStreamStatusUpdateAsync(string subscriptionType, string channelName)
    {
        // Map event type to target status and required prior state
        var (newStatus, expectedStatus) = subscriptionType switch
        {
            "stream.online" => (StreamStatus.Live, StreamStatus.Scheduled),
            "stream.offline" => (StreamStatus.Ended, StreamStatus.Live),
            _ => throw new ArgumentException($"Unsupported subscription type: {subscriptionType}")
        };

        if (newStatus == null || expectedStatus == null)
        {
            _logger.LogWarning("No state transition defined for subscription type {Type}.", subscriptionType);
            return null; // return null instead of false
        }
        // Resolve internal stream entity by Twitch channel name
        var stream = await _streamRepository.GetStreamByChannelNameAsync(channelName);
        if (stream == null)
        {
            _logger.LogWarning("Ignored status transition: No registered stream found for channel {ChannelName}.", channelName);
            return null; // return null instead of false
        }
        // Execute conditional transition on streams table
        var streamUpdated = await _streamRepository.UpdateStreamStatusAsync(stream.StreamId, newStatus, expectedStatus);
        if (!streamUpdated)
        {
            // Log out-of-order or duplicate transitions when 0 rows were affected
            _logger.LogWarning(
                "Ignored out-of-order transition: Stream ID {StreamId} attempted '{AttemptedStatus}', but stream was not in expected state '{ExpectedStatus}' (Current: '{CurrentStatus}').",
                stream.StreamId,
                newStatus,
                expectedStatus,
                stream.Status);
            return null; // return null instead of false
        }
        _logger.LogInformation(
            "Stream ID {StreamId} successfully transitioned from '{ExpectedStatus}' to '{NewStatus}'.",
            stream.StreamId,
            expectedStatus,
            newStatus);
        // Cascade transition to linked match fixture if present
        if (stream.MatchId.HasValue)
        {
            var matchUpdated = await _matchRepository.UpdateMatchStatusAsync(stream.MatchId.Value, newStatus, expectedStatus);
            if (matchUpdated)
            {
                _logger.LogInformation(
                    "Match ID {MatchId} successfully transitioned from '{ExpectedStatus}' to '{NewStatus}'.",
                    stream.MatchId.Value,
                    expectedStatus,
                    newStatus);
            }
        }
        return stream.StreamId; // return stream ID for audit log linking
    }
}