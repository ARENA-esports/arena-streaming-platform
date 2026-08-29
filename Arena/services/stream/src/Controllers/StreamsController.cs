using System.Security.Claims;   // import identity and claim types for extract identity from tokens
using Microsoft.AspNetCore.Authorization;   // provide security attributes
using Microsoft.AspNetCore.Mvc;     //import ASP.NET core Mvc framework types
using StreamService.DTOs;
using StreamService.Repositories;


namespace StreamService.Controllers;

[ApiController]
[Route("api")]
public class StreamsController : ControllerBase
{
    private readonly IStreamRepository _streamRepository;
    private readonly IMatchRepository _matchRepository;

    /* constructor with dependency injection */
    public StreamsController(IStreamRepository streamRepository, IMatchRepository matchRepository)
    {
        _streamRepository = streamRepository;
        _matchRepository = matchRepository;
    }
    /* implement post route & security  */
    [HttpPost("matches/{matchId:int}/streams")] //maps the endpoint with an inline integer route constraint
    [Authorize(Roles="Organizer,Streamer")] //restricts execution to users
    [ProducesResponseType(typeof(StreamResponse),StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkStreamToMatch(int matchId,[FromBody] LinkStreamRequest request)
    {
        // securely extract and validate user identity from JWT claims
        var UserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value    // look for standard claim for user's unique identifier and safely get value else get null
                          ?? User.FindFirst("sub")?.Value;      // if first lookup fail, fallback to sub claim
        /*  check if claim is missing or empty attempt to convert claim to an integer 
            if success streamer id hold value. else send 401 response*/
        if(string.IsNullOrEmpty(UserIdClaim)|| !int.TryParse(UserIdClaim, out var streamerId))
        {
            return Unauthorized(new{message = "Invalid or missing user identity claim in token."});
        }

        /* validate target match exists */
        var match = await _matchRepository.GetMatchByIdAsync(matchId);
        if (match == null)
        {
            return NotFound(new {message=$"Match with ID {matchId} does not exist."});
        }
        /* stream-to-match constraint */
        var alreadyLinked = await _streamRepository.StreamExistsForMatchAsync(matchId);
        if (alreadyLinked)
        {
            return Conflict(new{message = $"A live stream broadcast is already linked to Match ID {matchId}."});
        }
        /* insert stream record into database */
        var streamId = await _streamRepository.LinkStreamToMatchAsync(
            matchId,
            streamerId,
            match.TournamentId,
            request
        );
        /* Fetch persisted record and return 201 Created with Location header */
        var createdStream = await _streamRepository.GetStreamByIdAsync(streamId);
        return CreatedAtAction(
            nameof(GetStreamById),
            new {streamId},
            createdStream
        );

    }

    /* public lookup endpoints */
    [HttpGet("streams/{streamId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StreamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStreamById(int streamId)
    {
        var stream = await _streamRepository.GetStreamByIdAsync(streamId);
        if (stream == null)
        {
            return NotFound(new{message = $"Stream with ID {streamId} not found."});
        }
        return Ok(stream);
    }

    [HttpGet("matches/{matchId:int}/stream")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StreamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStreamByMatchId(int matchId)
    {
        var stream = await _streamRepository.GetStreamByMatchIdAsync(matchId);
        if (stream == null)
        {
            return NotFound(new{message = $"No stream linked to Match ID {matchId}."});
        }
        return Ok(stream);
    }

    /* stream update endpoint */
    [HttpPut("streams/{streamId:int}")]
    [Authorize(Roles = "Organizer,Streamer")]
    [ProducesResponseType(typeof(StreamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStream(int streamId, [FromBody] UpdateStreamRequest request)
    {
        // Verify existence
        var existingStream = await _streamRepository.GetStreamByIdAsync(streamId);
        if (existingStream == null)
        {
            return NotFound(new { message = $"Stream with ID {streamId} not found." });
        }
        // Validate ownership or Organizer privileges
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("sub")?.Value;
        var isOrganizer = User.IsInRole("Organizer");
        //safely parse integer user id for ownership equality comparison against StreamerId
        if (!isOrganizer && (!int.TryParse(userIdClaim, out var currentUserId) || existingStream.StreamerId != currentUserId))
        {
            return Forbid();
        }
        // Update database record
        var updated = await _streamRepository.UpdateStreamAsync(streamId, request);
        if (!updated)
        {
            return BadRequest(new { message = "Failed to update stream." });
        }

        var result = await _streamRepository.GetStreamByIdAsync(streamId);
        return Ok(result);
    }

    /* stream delete endpoint */
    [HttpDelete("streams/{streamId:int}")]
    [Authorize(Roles = "Organizer,Streamer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStream(int streamId)
    {
        // Verify existence
        var existingStream = await _streamRepository.GetStreamByIdAsync(streamId);
        if (existingStream == null)
        {
            return NotFound(new { message = $"Stream with ID {streamId} not found." });
        }

        // Validate ownership or Organizer privileges
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("sub")?.Value;
        var isOrganizer = User.IsInRole("Organizer");
        if (!isOrganizer && (!int.TryParse(userIdClaim, out var currentUserId) || existingStream.StreamerId != currentUserId))
        {
            return Forbid();
        }

        // Delete stream
        await _streamRepository.DeleteStreamAsync(streamId);
        return NoContent();
    }

}
