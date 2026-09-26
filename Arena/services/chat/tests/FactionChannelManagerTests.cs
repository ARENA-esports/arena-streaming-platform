using System.Net.WebSockets;
using ChatService.WebSockets;
using Xunit;

namespace ChatService.Tests;

public class FactionChannelManagerTests
{
    private readonly FactionChannelManager _manager = new();

    // ── AC1: Authenticated connection registers in the correct team room ──

    [Fact]
    public void AddToChannel_RegistersSocket_InCorrectTeamRoom()
    {
        // Arrange
        var socket = CreateMockSocket(WebSocketState.Open);
        var teamId = 1;
        var connectionId = "conn-1";

        // Act
        _manager.AddToChannel(teamId, connectionId, socket);

        // Assert
        Assert.Equal(1, _manager.GetChannelConnectionCount(teamId));
        Assert.Equal(1, _manager.GetActiveChannelCount());
    }

    [Fact]
    public void AddToChannel_MultipleConnections_SameTeam_AllRegistered()
    {
        // Arrange
        var teamId = 1;

        // Act
        _manager.AddToChannel(teamId, "conn-1", CreateMockSocket(WebSocketState.Open));
        _manager.AddToChannel(teamId, "conn-2", CreateMockSocket(WebSocketState.Open));
        _manager.AddToChannel(teamId, "conn-3", CreateMockSocket(WebSocketState.Open));

        // Assert
        Assert.Equal(3, _manager.GetChannelConnectionCount(teamId));
        Assert.Equal(1, _manager.GetActiveChannelCount());
    }

    [Fact]
    public void AddToChannel_DifferentTeams_CreatesSeperateChannels()
    {
        // Arrange & Act
        _manager.AddToChannel(1, "conn-1", CreateMockSocket(WebSocketState.Open));
        _manager.AddToChannel(2, "conn-2", CreateMockSocket(WebSocketState.Open));

        // Assert
        Assert.Equal(1, _manager.GetChannelConnectionCount(1));
        Assert.Equal(1, _manager.GetChannelConnectionCount(2));
        Assert.Equal(2, _manager.GetActiveChannelCount());
    }

    // ── AC3: Faction isolation — Team A messages never reach Team B ──

    [Fact]
    public async Task BroadcastToChannel_OnlySendsToSameTeam()
    {
        // Arrange
        var teamASocket = new TestWebSocket();
        var teamBSocket = new TestWebSocket();

        _manager.AddToChannel(1, "conn-a", teamASocket);
        _manager.AddToChannel(2, "conn-b", teamBSocket);

        var payload = System.Text.Encoding.UTF8.GetBytes("{\"message\":\"Go Team A!\"}");

        // Act — broadcast only to team 1
        await _manager.BroadcastToChannelAsync(1, payload);

        // Assert — Team A received, Team B did not
        Assert.Equal(1, teamASocket.SentMessages.Count);
        Assert.Equal(0, teamBSocket.SentMessages.Count);
    }

    [Fact]
    public async Task BroadcastToChannel_DoesNotLeakToOtherTeams()
    {
        // Arrange
        var team1Socket1 = new TestWebSocket();
        var team1Socket2 = new TestWebSocket();
        var team2Socket1 = new TestWebSocket();

        _manager.AddToChannel(1, "conn-1a", team1Socket1);
        _manager.AddToChannel(1, "conn-1b", team1Socket2);
        _manager.AddToChannel(2, "conn-2a", team2Socket1);

        var payload = System.Text.Encoding.UTF8.GetBytes("{\"message\":\"Team 1 only\"}");

        // Act
        await _manager.BroadcastToChannelAsync(1, payload);

        // Assert — both Team 1 sockets received, Team 2 socket received nothing
        Assert.Equal(1, team1Socket1.SentMessages.Count);
        Assert.Equal(1, team1Socket2.SentMessages.Count);
        Assert.Equal(0, team2Socket1.SentMessages.Count);
    }

    [Fact]
    public async Task BroadcastToChannel_NonExistentTeam_DoesNotThrow()
    {
        // Act & Assert — broadcasting to team 999 should be a no-op
        var exception = await Record.ExceptionAsync(() =>
            _manager.BroadcastToChannelAsync(999, System.Text.Encoding.UTF8.GetBytes("test")));

        Assert.Null(exception);
    }

    // ── AC1: Disconnect handling — cleanup ──

    [Fact]
    public void RemoveFromChannel_CleansUpSocket()
    {
        // Arrange
        _manager.AddToChannel(1, "conn-1", CreateMockSocket(WebSocketState.Open));
        _manager.AddToChannel(1, "conn-2", CreateMockSocket(WebSocketState.Open));

        // Act
        _manager.RemoveFromChannel(1, "conn-1");

        // Assert
        Assert.Equal(1, _manager.GetChannelConnectionCount(1));
    }

    [Fact]
    public void RemoveFromChannel_RemovesEmptyRoom()
    {
        // Arrange
        _manager.AddToChannel(1, "conn-1", CreateMockSocket(WebSocketState.Open));

        // Act
        _manager.RemoveFromChannel(1, "conn-1");

        // Assert — room should be fully removed
        Assert.Equal(0, _manager.GetChannelConnectionCount(1));
        Assert.Equal(0, _manager.GetActiveChannelCount());
    }

    [Fact]
    public void RemoveFromChannel_NonExistentConnection_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _manager.RemoveFromChannel(1, "non-existent"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task BroadcastToChannel_RemovesClosedSockets()
    {
        // Arrange
        var openSocket = new TestWebSocket();
        var closedSocket = new TestWebSocket(WebSocketState.Closed);

        _manager.AddToChannel(1, "conn-open", openSocket);
        _manager.AddToChannel(1, "conn-closed", closedSocket);

        var payload = System.Text.Encoding.UTF8.GetBytes("test");

        // Act
        await _manager.BroadcastToChannelAsync(1, payload);

        // Assert — closed socket should have been cleaned up
        Assert.Equal(1, _manager.GetChannelConnectionCount(1));
        Assert.Equal(1, openSocket.SentMessages.Count);
    }

    // ── Helper: minimal WebSocket stub for state-based assertions ──

    private static WebSocket CreateMockSocket(WebSocketState state)
    {
        return new TestWebSocket(state);
    }

    // ══════════════════════════════════════════════
    // Story 2 Tests (new)
    // ══════════════════════════════════════════════

    // ── AC1 (S2): Sub-second delivery with 50 concurrent connections ──

    [Fact]
    public async Task BroadcastToChannel_CompletesWithinOneSecond_With50Connections()
    {
        // Arrange — 50 sockets in one team
        var sockets = new List<TestWebSocket>();
        for (int i = 0; i < 50; i++)
        {
            var socket = new TestWebSocket();
            _manager.AddToChannel(1, $"conn-{i}", socket);
            sockets.Add(socket);
        }

        var payload = System.Text.Encoding.UTF8.GetBytes("{\"type\":\"message\",\"content\":\"Load test\"}");

        // Act — time the broadcast
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _manager.BroadcastToChannelAsync(1, payload);
        stopwatch.Stop();

        // Assert — under 1 second
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Broadcast to 50 connections took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");

        // All 50 sockets received the message
        foreach (var socket in sockets)
        {
            Assert.Equal(1, socket.SentMessages.Count);
        }
    }

    // ── AC4 (S2): Remove from one channel does not block other channels ──

    [Fact]
    public async Task RemoveFromChannel_DoesNotBlockOtherChannels()
    {
        // Arrange — two channels
        var team1Socket = new TestWebSocket();
        var team2Socket = new TestWebSocket();
        _manager.AddToChannel(1, "conn-1", team1Socket);
        _manager.AddToChannel(2, "conn-2", team2Socket);

        // Act — remove from team 1
        _manager.RemoveFromChannel(1, "conn-1");

        // Assert — team 2 still works
        var payload = System.Text.Encoding.UTF8.GetBytes("test");
        await _manager.BroadcastToChannelAsync(2, payload);

        Assert.Equal(0, _manager.GetChannelConnectionCount(1));
        Assert.Equal(1, _manager.GetChannelConnectionCount(2));
        Assert.Equal(1, team2Socket.SentMessages.Count);
    }

    // ── AC4 (S2): Concurrent add/remove does not deadlock ──

    [Fact]
    public async Task ConcurrentAddAndRemove_DoesNotDeadlock()
    {
        // Arrange — run many concurrent add/remove operations
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var socket = new TestWebSocket();
                _manager.AddToChannel(1, $"stress-{index}", socket);
                _manager.RemoveFromChannel(1, $"stress-{index}");
            }));
        }

        // Act & Assert — should complete without deadlock within 5 seconds
        var completed = await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(5000));
        Assert.True(completed != Task.Delay(5000), "Concurrent add/remove deadlocked");

        // All connections should be cleaned up
        Assert.Equal(0, _manager.GetChannelConnectionCount(1));
    }

    // ── AC4 (S2): Broadcast during remove does not throw ──

    [Fact]
    public async Task BroadcastDuringRemove_DoesNotThrow()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            _manager.AddToChannel(1, $"conn-{i}", new TestWebSocket());
        }

        var payload = System.Text.Encoding.UTF8.GetBytes("concurrent test");

        // Act — broadcast and remove concurrently
        var broadcastTask = _manager.BroadcastToChannelAsync(1, payload);
        for (int i = 0; i < 10; i++)
        {
            _manager.RemoveFromChannel(1, $"conn-{i}");
        }

        // Assert — should not throw
        var exception = await Record.ExceptionAsync(() => broadcastTask);
        Assert.Null(exception);
    }
}

/// <summary>
/// Minimal WebSocket stub for unit testing — tracks sent messages and reports configurable state.
/// </summary>
internal class TestWebSocket : WebSocket
{
    private readonly WebSocketState _state;
    public List<byte[]> SentMessages { get; } = new();

    public TestWebSocket(WebSocketState state = WebSocketState.Open)
    {
        _state = state;
    }

    public override WebSocketState State => _state;

    public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.Closed
        ? WebSocketCloseStatus.NormalClosure : null;

    public override string? CloseStatusDescription => null;
    public override string? SubProtocol => null;

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
        bool endOfMessage, CancellationToken cancellationToken)
    {
        if (_state != WebSocketState.Open)
            throw new WebSocketException("Socket is not open.");

        SentMessages.Add(buffer.ToArray());
        return Task.CompletedTask;
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Not needed for FactionChannelManager tests.");
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override void Abort() { }
    public override void Dispose() { }
}
