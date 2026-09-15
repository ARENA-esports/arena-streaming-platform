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
    /// Retrieves a list of all registered teams with branding colors and logos.
    /// Publicly accessible to any viewer.
    /// </summary>
    /// <returns>List of registered teams with 200 OK.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<TeamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTeams()
    {
        var teams = await _teamRepository.GetAllTeamsAsync();
        return Ok(teams);
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
    /// Updates an existing team's name and/or color hex code.
    /// Restricted to users with the Organizer role.
    /// </summary>
    /// <param name="id">Team identifier.</param>
    /// <param name="request">Team update payload containing team_name and/or color_hex.</param>
    /// <returns>Updated team details with 200 OK.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organizer")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null || (request.TeamName == null && request.ColorHex == null))
        {
            return BadRequest(new { message = "At least one field ('team_name' or 'color_hex') must be provided for update." });
        }

        if (request.TeamName != null && string.IsNullOrWhiteSpace(request.TeamName))
        {
            return BadRequest(new { message = "Team name cannot be empty or whitespace." });
        }

        string? trimmedColor = null;
        if (request.ColorHex != null)
        {
            trimmedColor = request.ColorHex.Trim();
            if (!HexColorPattern.IsMatch(trimmedColor))
            {
                return BadRequest(new
                {
                    message = "Color hex code must be a valid 7-character hexadecimal format starting with '#' (e.g., #FF0055)."
                });
            }
        }

        var existingTeam = await _teamRepository.GetTeamByIdAsync(id);
        if (existingTeam == null)
        {
            return NotFound(new { message = $"Team with ID {id} not found." });
        }

        string? trimmedName = request.TeamName?.Trim();

        try
        {
            var updated = await _teamRepository.UpdateTeamAsync(id, trimmedName, trimmedColor);
            if (!updated)
            {
                return NotFound(new { message = $"Team with ID {id} not found." });
            }

            _logger.LogInformation("Team {TeamId} details updated", id);

            var result = await _teamRepository.GetTeamByIdAsync(id);
            return Ok(result);
        }
        catch (TeamConflictException ex)
        {
            _logger.LogWarning("Duplicate team name collision for team {TeamId}: {Message}", id, ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a team by its unique identifier along with its active roster of players.
    /// Publicly accessible to authenticated and unauthenticated viewers.
    /// </summary>
    /// <param name="id">Team identifier.</param>
    /// <returns>Team details with active roster if found, or 404 Not Found.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TeamDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamById(int id)
    {
        var team = await _teamRepository.GetTeamWithRosterAsync(id);
        if (team == null)
        {
            return NotFound(new { message = $"Team with ID {id} not found." });
        }

        return Ok(team);
    }

    /// <summary>
    /// Adds a player to a team's roster with an assigned role or position.
    /// Restricted to users with the Organizer role.
    /// </summary>
    /// <param name="id">Team identifier.</param>
    /// <param name="request">Payload containing player name/username and optional role/position.</param>
    /// <returns>Assigned player record with 201 Created containing player_id.</returns>
    [HttpPost("{id:int}/players")]
    [Authorize(Roles = "Organizer")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPlayer(int id, [FromBody] AddPlayerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null || string.IsNullOrWhiteSpace(request.EffectiveUsername))
        {
            return BadRequest(new { message = "Player username or name is required." });
        }

        var team = await _teamRepository.GetTeamByIdAsync(id);
        if (team == null)
        {
            return NotFound(new { message = $"Team with ID {id} not found." });
        }

        var playerId = await _teamRepository.AddPlayerToTeamAsync(id, request.EffectiveUsername, request.EffectiveRole);
        _logger.LogInformation("Player {PlayerId} '{Username}' added to team {TeamId}", playerId, request.EffectiveUsername, id);

        var playerResponse = new PlayerResponse(playerId, id, request.EffectiveUsername, request.EffectiveRole, true);
        return CreatedAtAction(nameof(GetTeamById), new { id }, playerResponse);
    }

    /// <summary>
    /// Removes a player from a team's roster.
    /// Restricted to users with the Organizer role.
    /// </summary>
    /// <param name="id">Team identifier.</param>
    /// <param name="playerId">Player identifier.</param>
    /// <returns>204 No Content on successful removal.</returns>
    [HttpDelete("{id:int}/players/{playerId:int}")]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePlayer(int id, int playerId)
    {
        var team = await _teamRepository.GetTeamByIdAsync(id);
        if (team == null)
        {
            return NotFound(new { message = $"Team with ID {id} not found." });
        }

        var removed = await _teamRepository.RemovePlayerFromTeamAsync(id, playerId);
        if (!removed)
        {
            return NotFound(new { message = $"Player with ID {playerId} not found on team {id}." });
        }

        _logger.LogInformation("Player {PlayerId} removed from team {TeamId}", playerId, id);
        return NoContent();
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
