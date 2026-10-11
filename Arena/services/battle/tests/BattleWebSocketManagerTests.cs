using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BattleEconomyService.DTOs;
using BattleEconomyService.WebSockets;
using Xunit;

namespace BattleEconomyService.Tests;

public class BattleWebSocketManagerTests
{
    private readonly BattleWebSocketManager _manager = new();

    [Fact]
    public void AddConnection_RegistersSocketInMatchRoom()
    {
        // Arrange
        var socket = new TestBattleWebSocket(WebSocketState.Open);
        var matchId = 10;
        var connectionId = "conn-1";

        // Act
        _manager.AddConnection(matchId, connectionId, socket);

        // Assert
        Assert.Equal(1, _manager.GetConnectionCount(matchId));
        Assert.Equal(1, _manager.GetActiveMatchCount());
    }

    [Fact]
    public void AddConnection_MultipleConnections_SameMatch_AllRegistered()
    {
        // Arrange
        var matchId = 10;

        // Act
        _manager.AddConnection(matchId, "c1", new TestBattleWebSocket(WebSocketState.Open));
        _manager.AddConnection(matchId, "c2", new TestBattleWebSocket(WebSocketState.Open));
        _manager.AddConnection(matchId, "c3", new TestBattleWebSocket(WebSocketState.Open));

        // Assert
        Assert.Equal(3, _manager.GetConnectionCount(matchId));
        Assert.Equal(1, _manager.GetActiveMatchCount());
    }

    [Fact]
    public void AddConnection_DifferentMatches_CreatesIsolatedRooms()
    {
        // Arrange & Act
        _manager.AddConnection(10, "c1", new TestBattleWebSocket(WebSocketState.Open));
        _manager.AddConnection(20, "c2", new TestBattleWebSocket(WebSocketState.Open));

        // Assert
        Assert.Equal(1, _manager.GetConnectionCount(10));
        Assert.Equal(1, _manager.GetConnectionCount(20));
        Assert.Equal(2, _manager.GetActiveMatchCount());
    }

    [Fact]
    public void RemoveConnection_RemovesSocketAndPrunesEmptyRoom()
    {
        // Arrange
        var matchId = 10;
        _manager.AddConnection(matchId, "c1", new TestBattleWebSocket(WebSocketState.Open));
        _manager.AddConnection(matchId, "c2", new TestBattleWebSocket(WebSocketState.Open));

        // Act
        _manager.RemoveConnection(matchId, "c1");

        // Assert
        Assert.Equal(1, _manager.GetConnectionCount(matchId));

        _manager.RemoveConnection(matchId, "c2");
        Assert.Equal(0, _manager.GetConnectionCount(matchId));
        Assert.Equal(0, _manager.GetActiveMatchCount());
    }

    [Fact]
    public async Task BroadcastToMatchAsync_DeliversPayloadToOpenSocketsInSameMatch()
    {
        // Arrange
        var socket1 = new TestBattleWebSocket(WebSocketState.Open);
        var socket2 = new TestBattleWebSocket(WebSocketState.Open);
        var matchId = 42;

        _manager.AddConnection(matchId, "c1", socket1);
        _manager.AddConnection(matchId, "c2", socket2);

        var message = new BattleBarBroadcastMessage
        {
            Type = "battle_bar_update",
            MatchId = matchId,
            Bars = new List<BattleBarDto>
            {
                new() { TeamId = 1, TotalDamage = 150 },
                new() { TeamId = 2, TotalDamage = 200 }
            },
            LatestAttack = new AttackEventDto
            {
                TeamId = 1,
                Damage = 6,
                WeaponName = "Hammer",
                WeaponId = 3
            }
        };

        // Act
        await _manager.BroadcastToMatchAsync(matchId, message);

        // Assert
        Assert.Single(socket1.SentMessages);
        Assert.Single(socket2.SentMessages);

        var receivedJson = Encoding.UTF8.GetString(socket1.SentMessages[0]);
        var received = JsonSerializer.Deserialize<JsonElement>(receivedJson);

        Assert.Equal("battle_bar_update", received.GetProperty("type").GetString());
        Assert.Equal(42, received.GetProperty("matchId").GetInt32());
        Assert.Equal(2, received.GetProperty("bars").GetArrayLength());
        Assert.Equal("Hammer", received.GetProperty("latestAttack").GetProperty("weaponName").GetString());
    }

    [Fact]
    public async Task BroadcastToMatchAsync_DoesNotLeakToOtherMatches()
    {
        // Arrange — Match 10 vs Match 20
        var match10Socket = new TestBattleWebSocket(WebSocketState.Open);
        var match20Socket = new TestBattleWebSocket(WebSocketState.Open);

        _manager.AddConnection(10, "c10", match10Socket);
        _manager.AddConnection(20, "c20", match20Socket);

        var message = new BattleBarBroadcastMessage
        {
            Type = "battle_bar_update",
            MatchId = 10,
            Bars = new List<BattleBarDto> { new() { TeamId = 1, TotalDamage = 50 } }
        };

        // Act — broadcast only to Match 10
        await _manager.BroadcastToMatchAsync(10, message);

        // Assert — Match 10 received, Match 20 did not
        Assert.Single(match10Socket.SentMessages);
        Assert.Empty(match20Socket.SentMessages);
    }

    [Fact]
    public async Task BroadcastToMatchAsync_ClosedOrFaultedSockets_PrunedAutomatically()
    {
        // Arrange
        var openSocket = new TestBattleWebSocket(WebSocketState.Open);
        var closedSocket = new TestBattleWebSocket(WebSocketState.Closed);
        var matchId = 10;

        _manager.AddConnection(matchId, "open-1", openSocket);
        _manager.AddConnection(matchId, "closed-1", closedSocket);

        var message = new BattleBarBroadcastMessage
        {
            Type = "battle_bar_update",
            MatchId = matchId
        };

        // Act
        await _manager.BroadcastToMatchAsync(matchId, message);

        // Assert — open socket got message, closed socket was pruned
        Assert.Single(openSocket.SentMessages);
        Assert.Equal(1, _manager.GetConnectionCount(matchId));
    }

    [Fact]
    public async Task BroadcastToMatchAsync_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var matchId = 99;
        var tasks = new List<Task>();

        // Add 30 sockets concurrently
        for (int i = 0; i < 30; i++)
        {
            var id = $"c-{i}";
            tasks.Add(Task.Run(() => _manager.AddConnection(matchId, id, new TestBattleWebSocket(WebSocketState.Open))));
        }
        await Task.WhenAll(tasks);

        // Concurrently broadcast and remove sockets
        var broadcastTask = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                await _manager.BroadcastToMatchAsync(matchId, new BattleBarBroadcastMessage { MatchId = matchId });
            }
        });

        var removeTask = Task.Run(() =>
        {
            for (int i = 0; i < 15; i++)
            {
                _manager.RemoveConnection(matchId, $"c-{i}");
            }
        });

        await Task.WhenAll(broadcastTask, removeTask);

        // Assert — no exceptions and room state is consistent
        Assert.Equal(15, _manager.GetConnectionCount(matchId));
    }
}

/// <summary>
/// Test double for WebSocket to inspect transmitted byte payloads in unit tests.
/// </summary>
internal class TestBattleWebSocket : WebSocket
{
    private readonly WebSocketState _state;
    public List<byte[]> SentMessages { get; } = new();

    public TestBattleWebSocket(WebSocketState state = WebSocketState.Open)
    {
        _state = state;
    }

    public override WebSocketState State => _state;

    public override WebSocketCloseStatus? CloseStatus =>
        _state == WebSocketState.Closed ? WebSocketCloseStatus.NormalClosure : null;

    public override string? CloseStatusDescription => null;
    public override string? SubProtocol => null;

    public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken = default)
    {
        if (_state != WebSocketState.Open)
        {
            throw new WebSocketException("Socket is not open.");
        }

        SentMessages.Add(buffer.ToArray());
        return ValueTask.CompletedTask;
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        if (_state != WebSocketState.Open)
        {
            throw new WebSocketException("Socket is not open.");
        }

        SentMessages.Add(buffer.ToArray());
        return Task.CompletedTask;
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override void Abort() { }
    public override void Dispose() { }
}
