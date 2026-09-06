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

    /*
        update and delete methods
    */
    Task<bool> UpdateStreamAsync(int streamId, UpdateStreamRequest request);
    Task<bool> DeleteStreamAsync(int streamId);
    /* webhook lifecycle transitions */
    // update stream record to 'Live' and set started_at timestamp when broadcast begins
    Task<int?> UpdateStreamLiveStatusAsync(string channelName, DateTimeOffset startedAt);

    // update stream record to 'Ended' and set ended_at timestamp when broadcast terminates
    Task<int?> UpdateStreamOfflineStatusAsync(string channelName);
}