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
}