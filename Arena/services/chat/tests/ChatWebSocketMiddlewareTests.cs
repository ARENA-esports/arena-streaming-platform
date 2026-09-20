using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using ChatService.Repositories;
using ChatService.WebSockets;
using ChatService.Entities;
using Xunit;

namespace ChatService.Tests;

public class ChatWebSocketMiddlewareTests : IAsyncDisposable
{
    private readonly Mock<IChatMessageRepository> _mockRepo;
    private IHost? _host;

    public ChatWebSocketMiddlewareTests()
    {
        _mockRepo = new Mock<IChatMessageRepository>();
        _mockRepo.Setup(r => r.InsertAsync(It.IsAny<ChatMessage>()))
            .ReturnsAsync(1L);
        _mockRepo.Setup(r => r.GetRecentByTeamAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<ChatMessage>());
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    /// <summary>
    /// Builds a test server with the WebSocket middleware.
    /// When authenticateUser is true, injects a fake authenticated identity.
    /// </summary>
    private async Task<IHost> CreateTestHost(bool authenticateUser = false)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<FactionChannelManager>();
                    services.AddSingleton<IChatMessageRepository>(_mockRepo.Object);
                    services.AddRouting();
                    services.AddAuthorization();
                    services.AddAuthentication("Test")
                        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                            TestAuthHandler>("Test", options => { });
                });
                webBuilder.Configure(app =>
                {
                    app.UseWebSockets();

                    if (authenticateUser)
                    {
                        // Inject authenticated user into the pipeline for testing
                        app.Use(async (context, next) =>
                        {
                            var claims = new[]
                            {
                                new Claim(ClaimTypes.NameIdentifier, "42"),
                                new Claim(ClaimTypes.Name, "TestUser"),
                                new Claim(ClaimTypes.Role, "Viewer")
                            };
                            var identity = new ClaimsIdentity(claims, "Test");
                            context.User = new ClaimsPrincipal(identity);
                            await next();
                        });
                    }

                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapChatWebSocket();
                    });
                });
            })
            .StartAsync();

        _host = host;
        return host;
    }

    // ══════════════════════════════════════════════
    // Story 1 Tests (preserved)
    // ══════════════════════════════════════════════

    // ── AC2 (S1): Unauthenticated connection → 401 ──

    [Fact]
    public async Task Rejects_Unauthenticated_Connection_With401()
    {
        var host = await CreateTestHost(authenticateUser: false);
        var server = host.GetTestServer();

        var client = server.CreateClient();
        var response = await client.GetAsync("/ws/chat?teamId=1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── AC1 (S1): Missing teamId → 400 ──

    [Fact]
    public async Task Rejects_MissingTeamId_With400()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/ws/chat");

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400, got {response.StatusCode}");
    }

    // ── AC1 (S1): Valid authenticated WS request is accepted ──

    [Fact]
    public async Task Accepts_Authenticated_WebSocket_Connection()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        Assert.Equal(System.Net.WebSockets.WebSocketState.Open, socket.State);

        var channelManager = host.Services.GetRequiredService<FactionChannelManager>();
        Assert.Equal(1, channelManager.GetChannelConnectionCount(1));

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC4 (S1): Message is persisted on receive ──

    [Fact]
    public async Task PersistsMessage_OnReceive()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        // Consume the history frame first
        await ConsumeHistoryFrame(socket);

        var messageBytes = Encoding.UTF8.GetBytes("Hello faction!");

        await socket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        await Task.Delay(200);

        _mockRepo.Verify(r => r.InsertAsync(It.Is<ChatMessage>(m =>
            m.TeamId == 1 &&
            m.UserId == 42 &&
            m.Username == "TestUser" &&
            m.Content == "Hello faction!"
        )), Times.Once);

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC3 (S1): Faction isolation across WebSocket connections ──

    [Fact]
    public async Task FactionIsolation_Messages_OnlyReachSameTeam()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var team1Socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);
        var team2Socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=2"), CancellationToken.None);

        // Consume history frames
        await ConsumeHistoryFrame(team1Socket);
        await ConsumeHistoryFrame(team2Socket);

        var channelManager = host.Services.GetRequiredService<FactionChannelManager>();
        Assert.Equal(1, channelManager.GetChannelConnectionCount(1));
        Assert.Equal(1, channelManager.GetChannelConnectionCount(2));

        var messageBytes = Encoding.UTF8.GetBytes("Team 1 only!");
        await team1Socket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        await Task.Delay(200);

        var buffer = new byte[4096];
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await team2Socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            Assert.Fail("Team 2 socket should not have received a message from Team 1's channel.");
        }
        catch (OperationCanceledException)
        {
            // Expected — team 2 received nothing (faction isolation working)
        }

        await team1Socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
        await team2Socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC1 (S1): Invalid teamId (zero/negative) → rejected ──

    [Fact]
    public async Task Rejects_InvalidTeamId_With400()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/ws/chat?teamId=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ══════════════════════════════════════════════
    // Story 2 Tests (new)
    // ══════════════════════════════════════════════

    // ── AC2 (S2): History hydration on join ──

    [Fact]
    public async Task SendsHistoryOnConnect_Last50Messages_ChronologicalOrder()
    {
        // Arrange — mock returns 3 messages in chronological order
        var historyMessages = new List<ChatMessage>
        {
            new() { MessageId = 1, TeamId = 1, UserId = 10, Username = "Alice", Content = "First message", CreatedAt = DateTime.UtcNow.AddMinutes(-3) },
            new() { MessageId = 2, TeamId = 1, UserId = 20, Username = "Bob", Content = "Second message", CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { MessageId = 3, TeamId = 1, UserId = 10, Username = "Alice", Content = "Third message", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _mockRepo.Setup(r => r.GetRecentByTeamAsync(1, 50))
            .ReturnsAsync(historyMessages);

        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        // Act — connect to team 1
        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        // Read the first frame (should be history)
        var buffer = new byte[8192];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("history", doc.RootElement.GetProperty("type").GetString());
        var messages = doc.RootElement.GetProperty("messages");
        Assert.Equal(3, messages.GetArrayLength());
        Assert.Equal("First message", messages[0].GetProperty("content").GetString());
        Assert.Equal("Second message", messages[1].GetProperty("content").GetString());
        Assert.Equal("Third message", messages[2].GetProperty("content").GetString());

        _mockRepo.Verify(r => r.GetRecentByTeamAsync(1, 50), Times.Once);

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task SendsEmptyHistoryOnConnect_WhenNoMessages()
    {
        // Arrange — default mock returns empty list
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        // Act
        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=99"), CancellationToken.None);

        var buffer = new byte[4096];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var doc = JsonDocument.Parse(json);

        // Assert — empty history
        Assert.Equal("history", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(0, doc.RootElement.GetProperty("messages").GetArrayLength());

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC3 (S2): Payload validation ──

    [Fact]
    public async Task RejectsEmptyMessage_WithErrorFrame()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        await ConsumeHistoryFrame(socket);

        // Act — send empty string
        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes("")),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        await Task.Delay(200);

        // Assert — receive error frame
        var (type, message) = await ReceiveTypedFrame(socket);
        Assert.Equal("error", type);
        Assert.Equal("Message cannot be empty.", message);

        // Verify message was NOT persisted
        _mockRepo.Verify(r => r.InsertAsync(It.IsAny<ChatMessage>()), Times.Never);

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task RejectsWhitespaceOnlyMessage_WithErrorFrame()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        await ConsumeHistoryFrame(socket);

        // Act — send whitespace only
        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes("   \t\n  ")),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        await Task.Delay(200);

        var (type, message) = await ReceiveTypedFrame(socket);
        Assert.Equal("error", type);
        Assert.Equal("Message cannot be empty.", message);

        _mockRepo.Verify(r => r.InsertAsync(It.IsAny<ChatMessage>()), Times.Never);

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task RejectsMessageOver500Chars_WithErrorFrame()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        await ConsumeHistoryFrame(socket);

        // Act — send a message that's 501 characters
        var longMessage = new string('A', 501);
        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(longMessage)),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        await Task.Delay(200);

        var (type, message) = await ReceiveTypedFrame(socket);
        Assert.Equal("error", type);
        Assert.Equal("Message exceeds 500 character limit.", message);

        _mockRepo.Verify(r => r.InsertAsync(It.IsAny<ChatMessage>()), Times.Never);

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task ValidMessage_IsBroadcast_WithTypeField()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        await ConsumeHistoryFrame(socket);

        // Act — send a valid message
        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes("Valid message!")),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        // Read the broadcast frame
        var buffer = new byte[4096];
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var doc = JsonDocument.Parse(json);

        // Assert — broadcast has type "message"
        Assert.Equal("message", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("Valid message!", doc.RootElement.GetProperty("content").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("userId").GetInt32());
        Assert.Equal("TestUser", doc.RootElement.GetProperty("username").GetString());

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC1 (S2): Sub-second delivery ──

    [Fact]
    public async Task BroadcastDelivery_CompletesWithinOneSecond()
    {
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        await ConsumeHistoryFrame(socket);

        // Act — send a message and measure round-trip
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await socket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes("Latency test")),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        var buffer = new byte[4096];
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

        stopwatch.Stop();

        // Assert — delivery within 1 second (AC1 NFR)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Broadcast delivery took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");

        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ══════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════

    /// <summary>
    /// Consumes the initial history frame that is sent on every connection.
    /// Call this after connecting before sending/receiving other frames.
    /// </summary>
    private static async Task ConsumeHistoryFrame(System.Net.WebSockets.WebSocket socket)
    {
        var buffer = new byte[8192];
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
    }

    /// <summary>
    /// Reads a single typed JSON frame and extracts the type and message fields.
    /// </summary>
    private static async Task<(string type, string message)> ReceiveTypedFrame(System.Net.WebSockets.WebSocket socket)
    {
        var buffer = new byte[4096];
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var doc = JsonDocument.Parse(json);

        var type = doc.RootElement.GetProperty("type").GetString() ?? "";
        var message = doc.RootElement.TryGetProperty("message", out var msgProp)
            ? msgProp.GetString() ?? ""
            : "";

        return (type, message);
    }
}

/// <summary>
/// Test authentication handler that passes through without real JWT validation.
/// Authentication is controlled by injecting ClaimsIdentity in the middleware.
/// </summary>
internal class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<
    Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identity?.IsAuthenticated == true)
        {
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(
                Context.User, "Test");
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
    }
}
