using StreamService.DTOs;

namespace StreamService.Repositories;
public interface IStreamRepository
{
    /*
        validation and conflict checks
    */
    Task<bool> MatchExistsAsync(int matchId);
    Task<bool> StreamExistsForMatchAsync(int matchId);
    /*
        stream insertion
    */
    Task<int> LinkStreamToMatchAsync(int matchId, int streamerId, int tournamentId, LinkStreamRequest request);
    /*
        lookup query method
    */
    Task<StreamResponse?> GetStreamByIdAsync(int streamId);
    Task<StreamResponse?> GetStreamByMatchIdAsync(int matchId);
    Task<StreamResponse?> GetStreamByChannelNameAsync(string channelName);  // fetches the stream by channel name to resolve stream_id and linked match_id
    /*
        update and delete methods
    */
    Task<bool> UpdateStreamAsync(int streamId, UpdateStreamRequest request);
    Task<bool> DeleteStreamAsync(int streamId);
    Task<bool> UpdateStreamStatusAsync(int streamId, string newStatus, string ExpectedCurrentStatus);   // conditional state-machine update enforcing expectedCurrentStatus
    /* webhook lifecycle transitions */
    // update stream record to 'Live' and set started_at timestamp when broadcast begins
    Task<int?> UpdateStreamLiveStatusAsync(string channelName, DateTimeOffset startedAt);

    // update stream record to 'Ended' and set ended_at timestamp when broadcast terminates
    Task<int?> UpdateStreamOfflineStatusAsync(string channelName);
}