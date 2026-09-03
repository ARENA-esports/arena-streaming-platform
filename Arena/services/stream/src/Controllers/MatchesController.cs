using Microsoft.AspNetCore.Mvc;      // imports ASP.NET controller base
using Microsoft.AspNetCore.Authorization;   //import [Authorize] and role-based access control filters
using StreamService.DTOs;
using StreamService.Repositories;

namespace StreamService.Controllers;

[ApiController]     // enable auto model validation base on DTO data annotations
[Route("api/[controller]")]     // map route dynamically base on controller name prefix
public class MatchesController : ControllerBase
{
    private readonly IMatchRepository _matchRepository;     // hold repository reference securely, prevent modification
    public MatchesController(IMatchRepository matchRepository)  // ASP.NET Core Dependency Injection (DI) container. provide MatchRepository instance at run time
    {
        _matchRepository = matchRepository;
    }

    [HttpPost]      // map method to HTTP POST /api/matches
    [Authorize(Roles = "Organizer")]    // validate caller's JWT token. if token missing/expired -> 401 Unauthorized. if role is not Organizer -> 403 Forbidden
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status201Created)]   // document every possible HTTP status code and response body
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMatch([FromBody] CreateMatchRequest request) // read and deserialize JSON request into CreateMatchRequest DTO
    {
        if(request.TeamAId == request.TeamBId)  //Rejects invalid matches where both team IDs are identical
        {
            return BadRequest(new {message = "A match cannot be created between same teams."});
        }
        if (request.ScheduledTime < DateTimeOffset.UtcNow.AddMinutes(-5))   // Prevents organizers from scheduling fixtures in the past. allow 5 min clock skew buffer
        {
            return BadRequest(new {message = "Scheduled time cannot be in the past."});
        }
        // execute ADO.NET count query. If either team ID is missing from teams, it returns 400 Bad Request
        var teamsExist = await _matchRepository.BothTeamsExistAsync(request.TeamAId, request.TeamBId);
        if(!teamsExist)
        {
            return BadRequest(new {message = "One or both specified teams do not exist."});
        }

        /*
            create match. insert new match record into DB with status "Scheduled".
            get generated primary key and return full record into client
        */
        var matchId = await _matchRepository.CreateMatchAsync(
            request.TournamentId,
            request.TeamAId,
            request.TeamBId,
            request.ScheduledTime
        );

        // fetch newly created match. ensure auto generated values exist.
        var createdMatch = await _matchRepository.GetMatchByIdAsync(matchId);

        return CreatedAtAction(     // ASP.NET helper to return 201 created response
            nameof(GetMatchById),
            new {id=matchId},       // add location header /api/matches/10
            createdMatch
        );
    }

    /* get endpoint for lookup and location routing */
    [HttpGet("{id:int}")]   // map to GET /api/matches/{id} with inline route constraint to ensure {id} is valid int
    [AllowAnonymous]        // match view is publicly accessible to viewers and downstream microservices
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status200OK)]    // return MatchResponse object if match exist
    [ProducesResponseType(StatusCodes.Status404NotFound)]                     // return if match is not exist
    public async Task<IActionResult> GetMatchById(int id)
    {
        var match = await _matchRepository.GetMatchByIdAsync(id);   // repository lookup
        /*  not found handle.
            if match not found, send 404 not found with JSON message with id
        */
        if(match == null)
        {
            return NotFound(new {message = $"Match with ID {id} not found."});
        }
        return Ok(match);   // if match exist, return 200 OK with JSON object
    }
}
