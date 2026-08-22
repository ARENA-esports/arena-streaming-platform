/*
    contract for validate teams, insert match records, fetch match details
*/

using StreamService.DTOs;


namespace interface IMatchRepository
{
    Task<bool> BothTeamsExistAsync(int teamAId, int teamBId);
    Task<int> CreateMatchAsync(int TournamentId, int TeamAId, int TeamBId, DateTimeOffset ScheduledTime);
    Task<MatchResponse?> GetMatchByIdAsync(int matchId);
}