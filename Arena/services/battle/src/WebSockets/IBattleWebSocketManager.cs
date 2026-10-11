using System.Net.WebSockets;
using BattleEconomyService.DTOs;

namespace BattleEconomyService.WebSockets;

/// <summary>
/// Contract for managing active WebSocket connections grouped by match rooms
/// and broadcasting real-time battle bar updates.
/// </summary>
public interface IBattleWebSocketManager
{
    /// <summary>
    /// Registers a WebSocket connection into the specified match room.
    /// </summary>
    void AddConnection(int matchId, string connectionId, WebSocket socket);

    /// <summary>
    /// Removes a WebSocket connection from the specified match room.
    /// </summary>
    void RemoveConnection(int matchId, string connectionId);

    /// <summary>
    /// Broadcasts a battle bar update to all active viewers connected to the match room.
    /// Faulted or closed sockets are automatically pruned.
    /// </summary>
    Task BroadcastToMatchAsync(int matchId, BattleBarBroadcastMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of active WebSocket connections in a match room.
    /// </summary>
    int GetConnectionCount(int matchId);

    /// <summary>
    /// Returns the total number of active match rooms.
    /// </summary>
    int GetActiveMatchCount();
}
