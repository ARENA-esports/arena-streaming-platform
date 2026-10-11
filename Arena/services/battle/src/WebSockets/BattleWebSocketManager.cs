using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using BattleEconomyService.DTOs;

namespace BattleEconomyService.WebSockets;

/// <summary>
/// Thread-safe manager for WebSocket connections grouped by match rooms.
/// Handles connection lifecycle, multi-room partitioning, and fan-out broadcasting.
/// </summary>
public class BattleWebSocketManager : IBattleWebSocketManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // matchId -> { connectionId -> WebSocket }
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, WebSocket>> _rooms = new();
    private readonly ILogger<BattleWebSocketManager>? _logger;

    public BattleWebSocketManager(ILogger<BattleWebSocketManager>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void AddConnection(int matchId, string connectionId, WebSocket socket)
    {
        var room = _rooms.GetOrAdd(matchId, _ => new ConcurrentDictionary<string, WebSocket>());
        room.TryAdd(connectionId, socket);
        _logger?.LogDebug("WebSocket registered: match {MatchId}, connection {ConnectionId}. Room count: {Count}",
            matchId, connectionId, room.Count);
    }

    /// <inheritdoc />
    public void RemoveConnection(int matchId, string connectionId)
    {
        if (_rooms.TryGetValue(matchId, out var room))
        {
            room.TryRemove(connectionId, out _);

            if (room.IsEmpty)
            {
                _rooms.TryRemove(matchId, out _);
                _logger?.LogDebug("Pruned empty match room {MatchId}", matchId);
            }
        }
    }

    /// <inheritdoc />
    public async Task BroadcastToMatchAsync(int matchId, BattleBarBroadcastMessage message, CancellationToken cancellationToken = default)
    {
        if (!_rooms.TryGetValue(matchId, out var room) || room.IsEmpty)
        {
            _logger?.LogDebug("Broadcast skipped for match {MatchId}: no connected viewers.", matchId);
            return;
        }

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        var payload = new ReadOnlyMemory<byte>(jsonBytes);
        var deadConnections = new List<string>();

        foreach (var (connectionId, socket) in room)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to send WebSocket message to connection {ConnectionId} in match {MatchId}. Marking for removal.",
                        connectionId, matchId);
                    deadConnections.Add(connectionId);
                }
            }
            else
            {
                deadConnections.Add(connectionId);
            }
        }

        foreach (var connId in deadConnections)
        {
            RemoveConnection(matchId, connId);
        }
    }

    /// <inheritdoc />
    public int GetConnectionCount(int matchId)
    {
        return _rooms.TryGetValue(matchId, out var room) ? room.Count : 0;
    }

    /// <inheritdoc />
    public int GetActiveMatchCount()
    {
        return _rooms.Count;
    }
}
