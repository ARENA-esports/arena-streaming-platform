using System.Net.WebSockets;
using System.Security.Claims;
using System.Text.Json;
using BattleEconomyService.DTOs;
using BattleEconomyService.Repositories;

namespace BattleEconomyService.WebSockets;

/// <summary>
/// WebSocket endpoint middleware mapped to /ws/battle. Handles:
/// 1. WebSocket upgrade verification (HTTP 101)
/// 2. matchId parameter validation
/// 3. Immediate state hydration (pushes current battle bar values upon connection)
/// 4. Registration in <see cref="IBattleWebSocketManager"/>
/// 5. Keep-alive receive loop and graceful disconnect cleanup
/// </summary>
public static class BattleWebSocketMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void MapBattleWebSocket(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/battle", HandleWebSocketRequest);
        app.Map("/ws/battle/{matchId:int}", HandleWebSocketRequest);
    }

    private static async Task HandleWebSocketRequest(HttpContext context)
    {
        // 1. Verify WebSocket upgrade request
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection required.");
            return;
        }

        // 2. Parse matchId from route or query string
        int matchId = 0;
        if (context.Request.RouteValues.TryGetValue("matchId", out var routeVal) && routeVal != null)
        {
            _ = int.TryParse(routeVal.ToString(), out matchId);
        }

        if (matchId <= 0 && context.Request.Query.TryGetValue("matchId", out var queryVal))
        {
            _ = int.TryParse(queryVal.ToString(), out matchId);
        }

        if (matchId <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Valid matchId parameter is required.");
            return;
        }

        // 3. Extract user identity if available (optional for viewers)
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;
        var usernameClaim = context.User.FindFirst(ClaimTypes.Name)?.Value
                            ?? context.User.FindFirst("unique_name")?.Value
                            ?? "Guest";

        // 4. Accept WebSocket connection (HTTP 101 Switching Protocols)
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString("N");

        var webSocketManager = context.RequestServices.GetRequiredService<IBattleWebSocketManager>();
        var barRepository = context.RequestServices.GetRequiredService<IBattleBarRepository>();
        var logger = context.RequestServices.GetRequiredService<ILogger<BattleWebSocketManager>>();

        logger.LogInformation(
            "WebSocket connected: Viewer {Username} (User {UserId}) joined Match {MatchId} battle channel. ConnectionId={ConnectionId}",
            usernameClaim, userIdClaim ?? "anonymous", matchId, connectionId);

        webSocketManager.AddConnection(matchId, connectionId, socket);

        try
        {
            // 5. Initial state hydration — send current bar values immediately
            try
            {
                var currentBars = await barRepository.GetBarsForMatchAsync(matchId);
                var initMessage = new BattleBarBroadcastMessage
                {
                    Type = "init",
                    MatchId = matchId,
                    Bars = currentBars.Select(b => new BattleBarDto
                    {
                        TeamId = b.TeamId,
                        TotalDamage = b.TotalDamage
                    }).ToList(),
                    Timestamp = DateTime.UtcNow
                };

                var initBytes = JsonSerializer.SerializeToUtf8Bytes(initMessage, JsonOptions);
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(
                        new ReadOnlyMemory<byte>(initBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send initial bar hydration for match {MatchId} to connection {ConnectionId}.",
                    matchId, connectionId);
            }

            // 6. Receive loop: keep connection alive and detect client disconnect
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client requested close",
                        CancellationToken.None);
                    break;
                }
            }
        }
        catch (WebSocketException)
        {
            // Expected when client disconnects abruptly (e.g. browser tab closed)
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected exception in battle WebSocket loop for match {MatchId}, connection {ConnectionId}.",
                matchId, connectionId);
        }
        finally
        {
            webSocketManager.RemoveConnection(matchId, connectionId);
            logger.LogInformation("WebSocket disconnected: match {MatchId}, connection {ConnectionId}.",
                matchId, connectionId);

            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
                }
                catch
                {
                    // Ignore errors during final socket disposal
                }
            }
            socket.Dispose();
        }
    }
}
