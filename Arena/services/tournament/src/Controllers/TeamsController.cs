using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TournamentService.DTOs;
using TournamentService.Exceptions;
using TournamentService.Repositories;
using TournamentService.Services;

namespace TournamentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TeamsController : ControllerBase
{
    private static readonly Regex HexColorPattern = new(CreateTeamRequest.HexColorRegex, RegexOptions.Compiled);
    private readonly ITeamRepository _teamRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(
        ITeamRepository teamRepository,
        IFileStorageService fileStorageService,
        ILogger<TeamsController> logger)
    {
        _teamRepository = teamRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new team and assigns its faction color code.
    /// Restricted to users with the Organizer role.
    /// </summary>
    /// <param name="request">Team registration payload containing team_name and 7-character color_hex.</param>
    /// <returns>Assigned team record with 201 Created containing team_id.</returns>
    [HttpPost]
    [Authorize(Roles = "Organizer")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.TeamName))
        {
            return BadRequest(new { message = "Team name is required." });
        }

        var trimmedColor = request.ColorHex?.Trim() ?? string.Empty;
        if (!HexColorPattern.IsMatch(trimmedColor))
        {
            return BadRequest(new
            {
                message = "Color hex code must be a valid 7-character hexadecimal format starting with '#' (e.g., #FF0055)."
            });
        }

        try
        {
            var teamId = await _teamRepository.CreateTeamAsync(
                request.TeamName.Trim(),
                trimmedColor
            );

            _logger.LogInformation("Team {TeamId} '{TeamName}' registered with color {ColorHex}", teamId, request.TeamName, trimmedColor);

            var createdTeam = await _teamRepository.GetTeamByIdAsync(teamId)
                ?? new TeamResponse(teamId, request.TeamName.Trim(), trimmedColor);

            return CreatedAtAction(nameof(GetTeamById), new { id = teamId }, createdTeam);
        }
        catch (TeamConflictException ex)
        {
            _logger.LogWarning("Duplicate team registration rejected: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a team by its unique identifier.
    /// Publicly accessible to authenticated and unauthenticated users.
    /// </summary>
    /// <param name="id">Team identifier.</param>
    /// <returns>Team details if found, or 404 Not Found.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamById(int id)
    {
        var team = await _teamRepository.GetTeamByIdAsync(id);
        if (team == null)
        {
            return NotFound(new { message = $"Team with ID {id} not found." });
        }

        return Ok(team);
    }

    /// <summary>
    /// Uploads and assigns a visual logo for a specified team.
    /// Restricted to users with the Organizer role.
    /// </summary>
    /// <param name="id">Team identifier.</param>
    /// <param name="file">Multipart image file (PNG, JPEG, or SVG up to 2 MB).</param>
    /// <returns>Publicly accessible logo URL with 200 OK.</returns>
    [HttpPost("{id:int}/logo")]
    [Authorize(Roles = "Organizer")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadTeamLogoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadTeamLogo(int id, IFormFile? file)
    {
        var existingTeam = await _teamRepository.GetTeamByIdAsync(id);
        if (existingTeam == null)
        {
            return NotFound(new { message = $"Team with ID {id} not found." });
        }

        if (!_fileStorageService.ValidateTeamLogo(file, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        var publicUrl = await _fileStorageService.SaveFileAsync(file!, "logos", HttpContext.RequestAborted);

        var updated = await _teamRepository.UpdateTeamLogoAsync(id, publicUrl);
        if (!updated)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to update team logo in database." });
        }

        _logger.LogInformation("Team {TeamId} logo uploaded successfully: {LogoUrl}", id, publicUrl);

        return Ok(new UploadTeamLogoResponse(publicUrl, id));
    }
}
