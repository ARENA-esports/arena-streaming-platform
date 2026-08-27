namespace StreamService.DTOs;   // use c# file scope

public record StreamResponse
{
    int StreamId,
    int StreamerId,     // user identifier extracted from token
    int? TournamentId,
    int? MatchId,
    

}