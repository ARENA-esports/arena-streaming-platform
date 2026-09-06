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
    // Cascades status transitions (Scheduled -> Live, Live -> Ended) to the linked fixture
    Task<bool> UpdateMatchStatusAsync(int matchId, string newStatus, string expectedCurrentStatus);
}