using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BattleEconomyService.Repositories;

namespace BattleEconomyService.Controllers;

/// <summary>
/// Exposes wallet/balance information for the authenticated viewer (SCRUM-116).
/// </summary>
[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletRepository walletRepository, ILogger<WalletController> logger)
    {
        _walletRepository = walletRepository;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current coin balance for the authenticated viewer.
    /// Returns { balance: 0 } when the viewer has not yet earned any coins
    /// (wallet row does not exist yet).
    /// </summary>
    /// <returns>The viewer's current coin balance.</returns>
    /// <response code="200">Balance returned successfully.</response>
    /// <response code="401">Missing, expired, or invalid JWT authentication token.</response>
    [HttpGet("balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBalance()
    {
        // Extract authenticated user ID using the same established pattern as WatchTickController
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            _logger.LogWarning("Wallet balance request rejected: invalid or missing user ID claim.");
            return Unauthorized(new { message = "Invalid or missing user identity claim in token." });
        }

        // New viewers who have never ticked will not have a wallet row yet.
        // Return balance: 0 rather than 404 so the frontend always receives a valid number.
        var wallet = await _walletRepository.GetByUserIdAsync(userId);

        return Ok(new { balance = wallet?.Coins ?? 0 });
    }
}
