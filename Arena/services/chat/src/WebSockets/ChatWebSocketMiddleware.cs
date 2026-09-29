using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ChatService.Entities;
using ChatService.Repositories;
using ChatService.Services;

namespace ChatService.WebSockets;

/// <summary>
/// WebSocket middleware mapped to /ws/chat. Handles:
/// 1. JWT authentication (cookie or query param) — rejects with 401 if invalid
/// 2. teamId query-string validation — rejects with 400 if missing
/// 3. WebSocket upgrade (HTTP 101) on success
/// 4. History hydration — sends last 50 messages on connect
/// 5. Receive loop: validate → persist → broadcast
/// 6. Cleanup on disconnect with CancellationToken
/// </summary>
public static class ChatWebSocketMiddleware
{
    private const int MaxMessageLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void MapChatWebSocket(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/chat", async (HttpContext context) =>
        {
            // ── 1. Validate that this is a WebSocket request ──
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket connection required.");
                return;
            }

            // ── 2. Authenticate — JWT must be validated before upgrade ──
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authentication required.");
                return;
            }

            // ── 3. Parse teamId from query string ──
            if (!int.TryParse(context.Request.Query["teamId"], out var teamId) || teamId <= 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Valid teamId query parameter is required.");
                return;
            }

            // ── 4. Extract user claims ──
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? context.User.FindFirst("sub")?.Value;
            var usernameClaim = context.User.FindFirst(ClaimTypes.Name)?.Value
                                ?? context.User.FindFirst("unique_name")?.Value
                                ?? "Unknown";

            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid user identity in token.");
                return;
            }

            // ── 5. Accept WebSocket upgrade → HTTP 101 ──
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = Guid.NewGuid().ToString("N");

            var channelManager = context.RequestServices.GetRequiredService<FactionChannelManager>();
            var repository = context.RequestServices.GetRequiredService<IChatMessageRepository>();
            var teamCacheRepo = context.RequestServices.GetRequiredService<IChatTeamCacheRepository>();
            var muteRepo = context.RequestServices.GetRequiredService<IChatMuteRepository>();
            var profanityFilter = context.RequestServices.GetRequiredService<IProfanityFilter>();
            var logger = context.RequestServices.GetRequiredService<ILogger<FactionChannelManager>>();

            var teamInfo = await teamCacheRepo.GetByIdAsync(teamId) ?? new ChatTeamCache 
            { 
                TeamId = teamId, 
                TeamName = $"Team {teamId}", 
                TeamColor = "#FFFFFF" 
            };

            channelManager.AddToChannel(teamId, connectionId, socket);

            logger.LogInformation(
                "WebSocket connected: User {UserId} ({Username}) joined Team {TeamId} channel. ConnectionId={ConnectionId}",
                userId, usernameClaim, teamId, connectionId);

            try
            {
                // ── 6. History hydration — send last 50 messages on join (AC2) ──
                await SendHistoryAsync(socket, repository, teamInfo);

                // ── 7. Enter receive loop ──
                await HandleReceiveLoopAsync(socket, channelManager, repository, muteRepo, profanityFilter, teamInfo, connectionId, userId, usernameClaim, logger);
            }
            finally
            {
                // ── 8. Cleanup on disconnect (AC4) ──
                channelManager.RemoveFromChannel(teamId, connectionId);
                logger.LogInformation(
                    "WebSocket disconnected: User {UserId} left Team {TeamId} channel. ConnectionId={ConnectionId}",
                    userId, teamId, connectionId);

                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        // Timeout the close handshake to prevent deadlocking (AC4)
                        using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", closeCts.Token);
                    }
                    catch
                    {
                        // Socket already disposed, faulted, or close timed out — safe to ignore
                    }
                }

                socket.Dispose();
            }
        }).RequireAuthorization();
    }

    /// <summary>
    /// Sends the last 50 messages as a history payload to the newly connected client (AC2).
    /// </summary>
    private static async Task SendHistoryAsync(WebSocket socket, IChatMessageRepository repository, ChatTeamCache teamInfo)
    {
        var recentMessages = await repository.GetRecentByTeamAsync(teamInfo.TeamId, 50);

        var historyPayload = JsonSerializer.Serialize(new
        {
            type = "history",
            messages = recentMessages.Select(m => new
            {
                messageId = m.MessageId,
                teamId = m.TeamId,
                teamName = teamInfo.TeamName,
                teamColor = teamInfo.TeamColor,
                userId = m.UserId,
                username = m.Username,
                content = m.Content,
                createdAt = m.CreatedAt.ToString("o")
            })
        }, JsonOptions);

        var payloadBytes = Encoding.UTF8.GetBytes(historyPayload);

        if (socket.State == WebSocketState.Open)
        {
            await socket.SendAsync(payloadBytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    /// <summary>
    /// Receive loop: reads frames from the client, validates, persists messages, and broadcasts to the faction channel.
    /// </summary>
    private static async Task HandleReceiveLoopAsync(
        WebSocket socket,
        FactionChannelManager channelManager,
        IChatMessageRepository repository,
        IChatMuteRepository muteRepo,
        IProfanityFilter profanityFilter,
        ChatTeamCache teamInfo,
        string connectionId,
        int userId,
        string username,
        ILogger logger)
    {
        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // Client disconnected abruptly (AC4 — no deadlock, just break)
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                // ── Payload validation (AC3) ──

                // Reject empty or whitespace-only messages
                if (string.IsNullOrWhiteSpace(messageText))
                {
                    await SendErrorFrameAsync(socket, "Message cannot be empty.");
                    continue;
                }

                // Reject messages exceeding 500 characters
                if (messageText.Length > MaxMessageLength)
                {
                    await SendErrorFrameAsync(socket, "Message exceeds 500 character limit.");
                    continue;
                }

                // ── Mute check (Story 5, AC3 — Muted Message Drop) ──
                if (await muteRepo.IsUserMutedAsync(userId))
                {
                    await SendErrorFrameAsync(socket, "You are currently muted.");
                    continue;
                }

                // ── Profanity filter (Story 5, AC1 — Profanity Scrubbing) ──
                messageText = profanityFilter.Scrub(messageText);

                // ── Persist the message (AC4 from Story 1) ──
                var chatMessage = new ChatMessage
                {
                    TeamId = teamInfo.TeamId,
                    UserId = userId,
                    Username = username,
                    Content = messageText,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    var messageId = await repository.InsertAsync(chatMessage);
                    chatMessage.MessageId = messageId;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist message from User {UserId} in Team {TeamId}", userId, teamInfo.TeamId);
                    await SendErrorFrameAsync(socket, "Failed to send message. Please try again.");
                    continue;
                }

                // ── Build typed broadcast payload ──
                var broadcastPayload = JsonSerializer.Serialize(new
                {
                    type = "message",
                    messageId = chatMessage.MessageId,
                    teamId = chatMessage.TeamId,
                    teamName = teamInfo.TeamName,
                    teamColor = teamInfo.TeamColor,
                    userId = chatMessage.UserId,
                    username = chatMessage.Username,
                    content = chatMessage.Content,
                    createdAt = chatMessage.CreatedAt.ToString("o")
                }, JsonOptions);

                var payloadBytes = Encoding.UTF8.GetBytes(broadcastPayload);

                // Broadcast to all sockets in the faction channel (AC1 — sub-second async delivery)
                await channelManager.BroadcastToChannelAsync(teamInfo.TeamId, payloadBytes);
            }
        }
    }

    /// <summary>
    /// Sends an error frame back to the sender only. Not broadcast to the channel.
    /// </summary>
    private static async Task SendErrorFrameAsync(WebSocket socket, string errorMessage)
    {
        if (socket.State != WebSocketState.Open)
            return;

        var errorPayload = JsonSerializer.Serialize(new
        {
            type = "error",
            message = errorMessage
        }, JsonOptions);

        var payloadBytes = Encoding.UTF8.GetBytes(errorPayload);

        try
        {
            await socket.SendAsync(payloadBytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {
            // Socket died while sending error — ignore
        }
    }
}
