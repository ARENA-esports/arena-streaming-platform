/* 
    define exact JSON payload organize should send via POST /api/matches.
*/

using System.ComponentModel.DataAnnotations;    // import build in validation attributes


namespace StreamService.DTOs;

public class CreateMatchRequest{
    /*  add property for TournamentId. full tournament service will completed on sprint 2.
        initialize property to 1(default) -> requests without TournamentId still work.
        */
    public int TournamentId {get;set;} = 1;

    /*  declare team id properties. force ASP.NET to automatically check that both team ids are present and greater than 0,
        before request reach SQL query
    */
    [Required(ErrorMessage= "Team A ID is required.")]  // fail validation if cclient omits field entirely from JSON payload
    [Range(1, int.MaxValue, ErrorMessage = "Team A ID must be a valid positive integer.")]  // block invalid inputs(0, negative numbers) instantly
    public int TeamAId {get; set;}

    [Required(ErrorMessage= "Team B ID is required.")]  //custom error message with 400 Bad Request
    [Range(1, int.MaxValue, ErrorMessage = "Team B ID must be a valid positive integer.")]
    public int TeamBId {get; set;}

    // match timestamp property guarantee UTC offsets send from different users preserved accurately without local clock corruption
    [Required(ErrorMessage="Scheduled time is required.")]
    public DateTimeOffset ScheduledTime {get;set;}  // deserialization with format "2026-08-22T20:00:00Z"

}

