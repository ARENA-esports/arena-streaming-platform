using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace ChatService.WebSockets;

/// <summary>
/// Singleton service that manages WebSocket connections grouped by faction (team) channels.
/// Uses ConcurrentDictionary for thread-safe room multiplexing.
/// </summary>
public class FactionChannelManager
{
    // teamId → { connectionId → WebSocket }
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, WebSocket>> _channels = new();

    /// <summary>
    /// Registers a WebSocket connection in the specified team's faction channel.
    /// </summary>
    public void AddToChannel(int teamId, string connectionId, WebSocket socket)
    {
        var room = _channels.GetOrAdd(teamId, _ => new ConcurrentDictionary<string, WebSocket>());
        room.TryAdd(connectionId, socket);
    }

    /// <summary>
    /// Removes a WebSocket connection from the specified team's faction channel.
    /// Cleans up the room dictionary if it becomes empty.
    /// </summary>
    public void RemoveFromChannel(int teamId, string connectionId)
    {
        if (_channels.TryGetValue(teamId, out var room))
        {
            room.TryRemove(connectionId, out _);

            // Clean up empty rooms to prevent unbounded dictionary growth
            if (room.IsEmpty)
            {
                _channels.TryRemove(teamId, out _);
            }
        }
    }

    /// <summary>
    /// Broadcasts a message payload to all active sockets in the specified team's channel.
    /// Dead sockets are automatically removed on send failure.
    /// Only sockets in the target teamId receive the frame (faction isolation).
    /// </summary>
    public async Task BroadcastToChannelAsync(int teamId, ReadOnlyMemory<byte> payload)
    {
        if (!_channels.TryGetValue(teamId, out var room))
            return;

        var deadConnections = new List<string>();

        foreach (var (connectionId, socket) in room)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    // Socket failed — mark for removal
                    deadConnections.Add(connectionId);
                }
            }
            else
            {
                deadConnections.Add(connectionId);
            }
        }

        // Clean up dead connections outside the iteration
        foreach (var connId in deadConnections)
        {
            RemoveFromChannel(teamId, connId);
        }
    }

    /// <summary>
    /// Returns the number of active connections in a specific team channel.
    /// Useful for monitoring and testing.
    /// </summary>
    public int GetChannelConnectionCount(int teamId)
    {
        return _channels.TryGetValue(teamId, out var room) ? room.Count : 0;
    }

    /// <summary>
    /// Returns the total number of active faction channels.
    /// </summary>
    public int GetActiveChannelCount()
    {
        return _channels.Count;
    }
}
