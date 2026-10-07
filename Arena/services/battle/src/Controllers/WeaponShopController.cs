using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BattleEconomyService.DTOs;
using BattleEconomyService.Services;

namespace BattleEconomyService.Controllers;

/// <summary>
/// Weapon shop endpoints — browse weapons and purchase attacks (SCRUM-119).
/// </summary>
[ApiController]
[Route("api/economy")]
[Authorize]
public class WeaponShopController : ControllerBase
{
    private readonly IWeaponShopService _weaponShopService;
    private readonly ILogger<WeaponShopController> _logger;

    public WeaponShopController(IWeaponShopService weaponShopService, ILogger<WeaponShopController> logger)
    {
        _weaponShopService = weaponShopService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the list of active weapons available for purchase.
    /// </summary>
    /// <returns>List of active weapons with cost, damage, and icon information.</returns>
    /// <response code="200">Weapon catalog returned successfully.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    [HttpGet("weapons")]
    [ProducesResponseType(typeof(List<WeaponDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWeapons()
    {
        var weapons = await _weaponShopService.GetWeaponsAsync();
        return Ok(weapons);
    }

    /// <summary>
    /// Purchases a weapon attack for the authenticated viewer's team.
    /// Deducts coins and records the attack if the viewer has sufficient balance.
    /// </summary>
    /// <param name="request">Attack purchase payload containing weaponId, matchId, and teamId.</param>
    /// <returns>Attack result with updated balance on success, or error details on failure.</returns>
    /// <response code="200">Attack purchased and submitted successfully.</response>
    /// <response code="400">Invalid request, insufficient coins, or weapon not found.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    [HttpPost("attack")]
    [ProducesResponseType(typeof(AttackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AttackResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PurchaseAttack([FromBody] AttackRequest? request)
    {
        // 1. Resolve authenticated user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            _logger.LogWarning("Attack purchase rejected: invalid or missing user ID claim.");
            return Unauthorized(new { message = "Invalid or missing user identity claim in token." });
        }

        // 2. Validate request payload
        if (request == null)
        {
            return BadRequest(new AttackResponse
            {
                Success = false,
                Message = "Request body is required."
            });
        }

        if (request.WeaponId <= 0)
        {
            return BadRequest(new AttackResponse
            {
                Success = false,
                Message = "Invalid weapon ID. Must be a positive integer."
            });
        }

        if (request.MatchId <= 0)
        {
            return BadRequest(new AttackResponse
            {
                Success = false,
                Message = "Invalid match ID. Must be a positive integer."
            });
        }

        if (request.TeamId <= 0)
        {
            return BadRequest(new AttackResponse
            {
                Success = false,
                Message = "Invalid team ID. Must be a positive integer."
            });
        }

        // 3. Process purchase through business layer
        var result = await _weaponShopService.PurchaseAttackAsync(userId, request);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Returns the current battle bar state for all teams in a match.
    /// </summary>
    /// <param name="matchId">The match to query battle bars for.</param>
    /// <returns>List of battle bar entries with team damage totals.</returns>
    /// <response code="200">Battle bar data returned successfully.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    [HttpGet("battle-bar/{matchId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBattleBar(int matchId)
    {
        if (matchId <= 0)
        {
            return BadRequest(new { message = "Invalid match ID. Must be a positive integer." });
        }

        var bars = await _weaponShopService.GetBarsForMatchAsync(matchId);
        return Ok(bars);
    }
}
