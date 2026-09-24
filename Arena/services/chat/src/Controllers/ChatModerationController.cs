using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Entities;
using ChatService.Models;
using ChatService.Repositories;

namespace ChatService.Controllers;

/// <summary>
/// Chat moderation endpoints. Restricted to Organizer and Admin roles (AC4 — RBAC Enforcement).
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize(Roles = "Organizer,Admin")]
public class ChatModerationController : ControllerBase
{
    private readonly IChatMuteRepository _muteRepository;
    private readonly ILogger<ChatModerationController> _logger;

    public ChatModerationController(IChatMuteRepository muteRepository, ILogger<ChatModerationController> logger)
    {
        _muteRepository = muteRepository;
        _logger = logger;
    }

    /// <summary>
    /// Mutes a user for a specified duration (AC2 — User Mute Action).
    /// Only accessible by Organizer or Admin roles.
    /// </summary>
    /// <param name="request">Mute details: userId, durationMinutes, optional reason</param>
    /// <returns>200 OK with mute details on success</returns>
    [HttpPost("mute")]
    public async Task<IActionResult> MuteUser([FromBody] MuteUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Extract the moderator's userId from JWT claims
        var moderatorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(moderatorIdClaim, out var moderatorId))
        {
            return Unauthorized(new { message = "Invalid moderator identity in token." });
        }

        var mute = new ChatMute
        {
            UserId = request.UserId,
            MutedBy = moderatorId,
            Reason = request.Reason,
            ExpiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes)
        };

        try
        {
            var muteId = await _muteRepository.InsertAsync(mute);

            _logger.LogInformation(
                "User {UserId} muted by Moderator {ModeratorId} for {Duration} minutes. MuteId={MuteId}",
                request.UserId, moderatorId, request.DurationMinutes, muteId);

            return Ok(new
            {
                muteId,
                userId = request.UserId,
                mutedBy = moderatorId,
                reason = request.Reason,
                durationMinutes = request.DurationMinutes,
                expiresAt = mute.ExpiresAt.ToString("o")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mute user {UserId}", request.UserId);
            return StatusCode(500, new { message = "An error occurred while muting the user." });
        }
    }

    /// <summary>
    /// Gets the active mute status for a user.
    /// </summary>
    [HttpGet("mute/{userId}")]
    public async Task<IActionResult> GetMuteStatus(int userId)
    {
        if (userId <= 0)
        {
            return BadRequest(new { message = "UserId must be a positive integer." });
        }

        try
        {
            var activeMute = await _muteRepository.GetActiveMuteAsync(userId);

            if (activeMute == null)
            {
                return Ok(new { userId, isMuted = false });
            }

            return Ok(new
            {
                userId,
                isMuted = true,
                muteId = activeMute.MuteId,
                mutedBy = activeMute.MutedBy,
                reason = activeMute.Reason,
                mutedAt = activeMute.MutedAt.ToString("o"),
                expiresAt = activeMute.ExpiresAt.ToString("o")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mute status for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while checking mute status." });
        }
    }
}
