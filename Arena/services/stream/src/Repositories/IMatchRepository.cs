/*
    contract for validate teams, insert match records, fetch match details
*/

using StreamService.DTOs;
namespace StreamService.Repositories;

public interface IMatchRepository
{
    Task<bool> BothTeamsExistAsync(int teamAId, int teamBId);
    Task<int> CreateMatchAsync(int tournamentId, int teamAId, int teamBId, DateTimeOffset scheduledTime);
    Task<MatchResponse?> GetMatchByIdAsync(int matchId);
    Task<IEnumerable<MatchResponse>> GetAllMatchesAsync();
    // Cascades status transitions (Scheduled -> Live, Live -> Ended) to the linked match
    Task<bool> UpdateMatchStatusAsync(int matchId, string newStatus, string expectedCurrentStatus);
    
    // Administrative override to forcefully update match details
    Task<bool> UpdateMatchAsync(int matchId, int teamAId, int teamBId, DateTimeOffset scheduledTime);
    
    // Administrative override to forcefully change the status of a match, bypassing the state machine
    Task<bool> UpdateMatchStatusOverrideAsync(int matchId, string newStatus);

    // Completely deletes a match from the system
    Task<bool> DeleteMatchAsync(int matchId);
}