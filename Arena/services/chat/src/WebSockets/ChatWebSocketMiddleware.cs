using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ChatService.Entities;
using ChatService.Repositories;

namespace ChatService.WebSockets;

/// <summary>
/// WebSocket middleware mapped to /ws/chat. Handles:
/// 1. JWT authentication (cookie or query param) — rejects with 401 if invalid
/// 2. teamId query-string validation — rejects with 400 if missing
/// 3. WebSocket upgrade (HTTP 101) on success
/// 4. Receive loop: persist messages via repository, broadcast via FactionChannelManager
/// 5. Cleanup on disconnect
/// </summary>
public static class ChatWebSocketMiddleware
{
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
            channelManager.AddToChannel(teamId, connectionId, socket);

            var logger = context.RequestServices.GetRequiredService<ILogger<FactionChannelManager>>();
            logger.LogInformation(
                "WebSocket connected: User {UserId} ({Username}) joined Team {TeamId} channel. ConnectionId={ConnectionId}",
                userId, usernameClaim, teamId, connectionId);

            try
            {
                await HandleReceiveLoopAsync(context, socket, channelManager, teamId, connectionId, userId, usernameClaim);
            }
            finally
            {
                // ── 7. Cleanup on disconnect ──
                channelManager.RemoveFromChannel(teamId, connectionId);
                logger.LogInformation(
                    "WebSocket disconnected: User {UserId} left Team {TeamId} channel. ConnectionId={ConnectionId}",
                    userId, teamId, connectionId);

                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                    }
                    catch
                    {
                        // Socket already disposed or faulted — safe to ignore
                    }
                }

                socket.Dispose();
            }
        }).RequireAuthorization();
    }

    /// <summary>
    /// Receive loop: reads frames from the client, persists messages, and broadcasts to the faction channel.
    /// </summary>
    private static async Task HandleReceiveLoopAsync(
        HttpContext context,
        WebSocket socket,
        FactionChannelManager channelManager,
        int teamId,
        string connectionId,
        int userId,
        string username)
    {
        var buffer = new byte[4096];
        var repository = context.RequestServices.GetRequiredService<IChatMessageRepository>();

        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // Client disconnected abruptly
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                if (string.IsNullOrWhiteSpace(messageText))
                    continue;

                // Persist the message (AC4)
                var chatMessage = new ChatMessage
                {
                    TeamId = teamId,
                    UserId = userId,
                    Username = username,
                    Content = messageText,
                    CreatedAt = DateTime.UtcNow
                };

                var messageId = await repository.InsertAsync(chatMessage);
                chatMessage.MessageId = messageId;

                // Build broadcast payload
                var broadcastPayload = JsonSerializer.Serialize(new
                {
                    messageId = chatMessage.MessageId,
                    teamId = chatMessage.TeamId,
                    userId = chatMessage.UserId,
                    username = chatMessage.Username,
                    content = chatMessage.Content,
                    createdAt = chatMessage.CreatedAt.ToString("o")
                });

                var payloadBytes = Encoding.UTF8.GetBytes(broadcastPayload);

                // Broadcast to all sockets in the faction channel (AC3 — faction isolation)
                await channelManager.BroadcastToChannelAsync(teamId, payloadBytes);
            }
        }
    }
}
