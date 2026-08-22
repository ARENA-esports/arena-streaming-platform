/*
    shape of the data returned to client on 200 ok and 201 created
*/

namespace StreamService.DTOs;

public record MatchResponse(
    int MatchId,
    int TournamentId,
    int TeamAId,
    int TeamBId,
    DateTimeOffset ScheduledTime,
    string status,
    int? WinnerTeamId,  // nullable. when a match is just created, there is no winner yet
    DateTime CreatedAt
);
