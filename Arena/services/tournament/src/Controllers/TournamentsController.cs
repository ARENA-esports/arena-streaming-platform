using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentService.DTOs;
using TournamentService.Models;
using TournamentService.Repositories;

namespace TournamentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TournamentsController : ControllerBase
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ILogger<TournamentsController> _logger;

    public TournamentsController(ITournamentRepository tournamentRepository, ILogger<TournamentsController> logger)
    {
        _tournamentRepository = tournamentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tournament season.
    /// </summary>
    /// <param name="request">Tournament creation payload.</param>
    /// <returns>Assigned tournament details with 201 Created.</returns>
    [HttpPost]
    [Authorize(Roles = "Organizer")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TournamentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tournament name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SeasonIdentifier))
        {
            return BadRequest(new { message = "Season identifier is required." });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { message = "End date must be greater than or equal to start date." });
        }

        var tournamentId = await _tournamentRepository.CreateTournamentAsync(
            request.Name.Trim(),
            request.SeasonIdentifier.Trim(),
            request.StartDate,
            request.EndDate
        );

        _logger.LogInformation("Tournament {TournamentId} created with season {SeasonIdentifier}", tournamentId, request.SeasonIdentifier);

        var createdTournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId)
            ?? new TournamentResponse(
                tournamentId,
                request.Name.Trim(),
                request.SeasonIdentifier.Trim(),
                request.StartDate,
                request.EndDate,
                TournamentStatus.Scheduled
            );

        return CreatedAtAction(nameof(GetTournamentById), new { id = tournamentId }, createdTournament);
    }

    /// <summary>
    /// Retrieves a tournament by its unique identifier.
    /// Publicly accessible to authenticated and unauthenticated users.
    /// </summary>
    /// <param name="id">Tournament identifier.</param>
    /// <returns>Tournament details if found, or 404 Not Found.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TournamentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTournamentById(int id)
    {
        var tournament = await _tournamentRepository.GetTournamentByIdAsync(id);
        if (tournament == null)
        {
            return NotFound(new { message = $"Tournament with ID {id} not found." });
        }

        return Ok(tournament);
    }

    /// <summary>
    /// Retrieves all tournaments ordered by start date.
    /// Publicly accessible to authenticated and unauthenticated users.
    /// </summary>
    /// <returns>List of tournament seasons.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TournamentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTournaments()
    {
        var tournaments = await _tournamentRepository.GetAllTournamentsAsync();
        return Ok(tournaments);
    }

    /// <summary>
    /// Updates a tournament season's details.
    /// Rejected if tournament is in Cancelled or Completed status.
    /// </summary>
    /// <param name="id">Tournament identifier.</param>
    /// <param name="request">Updated tournament details.</param>
    /// <returns>Updated tournament record.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organizer")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TournamentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTournament(int id, [FromBody] UpdateTournamentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tournament name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SeasonIdentifier))
        {
            return BadRequest(new { message = "Season identifier is required." });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { message = "End date must be greater than or equal to start date." });
        }

        var existing = await _tournamentRepository.GetTournamentByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Tournament with ID {id} not found." });
        }

        if (existing.Status == TournamentStatus.Cancelled || existing.Status == TournamentStatus.Completed)
        {
            return BadRequest(new { message = $"Cannot update tournament in {existing.Status} status." });
        }

        var updated = await _tournamentRepository.UpdateTournamentAsync(
            id,
            request.Name.Trim(),
            request.SeasonIdentifier.Trim(),
            request.StartDate,
            request.EndDate
        );

        if (!updated)
        {
            return BadRequest(new { message = "Failed to update tournament because its status does not permit modifications." });
        }

        var result = await _tournamentRepository.GetTournamentByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cancels a tournament season using an atomic conditional update query.
    /// </summary>
    /// <param name="id">Tournament identifier.</param>
    /// <returns>Cancelled tournament record.</returns>
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType(typeof(TournamentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTournament(int id)
    {
        var existing = await _tournamentRepository.GetTournamentByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Tournament with ID {id} not found." });
        }

        if (existing.Status == TournamentStatus.Cancelled)
        {
            return BadRequest(new { message = "Tournament is already cancelled." });
        }

        if (existing.Status == TournamentStatus.Completed)
        {
            return BadRequest(new { message = "Cannot cancel a completed tournament." });
        }

        var success = await _tournamentRepository.CancelTournamentAsync(id);
        if (!success)
        {
            return BadRequest(new { message = $"Cannot transition tournament {id} from {existing.Status} to Cancelled." });
        }

        _logger.LogInformation("Tournament {TournamentId} successfully cancelled", id);

        var cancelledTournament = await _tournamentRepository.GetTournamentByIdAsync(id);
        return Ok(cancelledTournament);
    }
}
