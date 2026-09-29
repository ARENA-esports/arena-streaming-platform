using BattleEconomyService.Models;

namespace BattleEconomyService.DTOs;

/// <summary>
/// Result of querying StreamService for stream liveness.
/// Decouples HTTP status codes and transient network errors from business logic.
/// </summary>
public record StreamLivenessResult
{
    public int StreamId { get; init; }
    public bool IsLive { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static StreamLivenessResult Live(int streamId) =>
        new()
        {
            StreamId = streamId,
            IsLive = true,
            Status = StreamLiveStatus.Live,
            IsSuccess = true
        };

    public static StreamLivenessResult NotLive(int streamId, string status) =>
        new()
        {
            StreamId = streamId,
            IsLive = false,
            Status = status,
            IsSuccess = true
        };

    public static StreamLivenessResult NotFound(int streamId) =>
        new()
        {
            StreamId = streamId,
            IsLive = false,
            Status = "NotFound",
            IsSuccess = false,
            ErrorMessage = $"Stream with ID {streamId} not found."
        };

    public static StreamLivenessResult Failed(int streamId, string error) =>
        new()
        {
            StreamId = streamId,
            IsLive = false,
            Status = "Unavailable",
            IsSuccess = false,
            ErrorMessage = error
        };
}
