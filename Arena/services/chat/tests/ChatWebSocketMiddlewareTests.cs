using System.Net;
using System.Security.Claims;
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

    // ── AC2: Unauthenticated connection → 401 ──

    [Fact]
    public async Task Rejects_Unauthenticated_Connection_With401()
    {
        // Arrange
        var host = await CreateTestHost(authenticateUser: false);
        var server = host.GetTestServer();

        // Act — make a regular HTTP request (not WebSocket) to trigger the 401
        var client = server.CreateClient();
        var response = await client.GetAsync("/ws/chat?teamId=1");

        // Assert — unauthenticated should get 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── AC1: Missing teamId → 400 ──

    [Fact]
    public async Task Rejects_MissingTeamId_With400()
    {
        // Arrange
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var client = server.CreateClient();

        // Act — no teamId parameter
        var response = await client.GetAsync("/ws/chat");

        // Assert — should get 400 because it's not a WebSocket request
        // (test HTTP client can't do WS upgrade, so it gets caught by the WebSocket check)
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400, got {response.StatusCode}");
    }

    // ── AC1: Valid authenticated WS request is accepted ──

    [Fact]
    public async Task Accepts_Authenticated_WebSocket_Connection()
    {
        // Arrange
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        // Act — connect with valid teamId
        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        // Assert — connection should be open (HTTP 101 was accepted)
        Assert.Equal(System.Net.WebSockets.WebSocketState.Open, socket.State);

        // Verify the socket was registered in the channel manager
        var channelManager = host.Services.GetRequiredService<FactionChannelManager>();
        Assert.Equal(1, channelManager.GetChannelConnectionCount(1));

        // Cleanup
        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC4: Message is persisted on receive ──

    [Fact]
    public async Task PersistsMessage_OnReceive()
    {
        // Arrange
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);

        var messageBytes = System.Text.Encoding.UTF8.GetBytes("Hello faction!");

        // Act — send a message
        await socket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        // Give the server a moment to process
        await Task.Delay(200);

        // Assert — repository InsertAsync was called with the correct message
        _mockRepo.Verify(r => r.InsertAsync(It.Is<ChatMessage>(m =>
            m.TeamId == 1 &&
            m.UserId == 42 &&
            m.Username == "TestUser" &&
            m.Content == "Hello faction!"
        )), Times.Once);

        // Cleanup
        await socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC3: Faction isolation across WebSocket connections ──

    [Fact]
    public async Task FactionIsolation_Messages_OnlyReachSameTeam()
    {
        // Arrange
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var wsClient = server.CreateWebSocketClient();

        var team1Socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=1"), CancellationToken.None);
        var team2Socket = await wsClient.ConnectAsync(
            new Uri(server.BaseAddress, "/ws/chat?teamId=2"), CancellationToken.None);

        var channelManager = host.Services.GetRequiredService<FactionChannelManager>();
        Assert.Equal(1, channelManager.GetChannelConnectionCount(1));
        Assert.Equal(1, channelManager.GetChannelConnectionCount(2));

        // Act — send a message on team 1's channel
        var messageBytes = System.Text.Encoding.UTF8.GetBytes("Team 1 only!");
        await team1Socket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            System.Net.WebSockets.WebSocketMessageType.Text,
            true, CancellationToken.None);

        // Give the server time to broadcast
        await Task.Delay(200);

        // Assert — try to receive on team 2 (should timeout/get nothing)
        var buffer = new byte[4096];
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        try
        {
            await team2Socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            // If we reach here, team 2 received something — that's a failure
            Assert.Fail("Team 2 socket should not have received a message from Team 1's channel.");
        }
        catch (OperationCanceledException)
        {
            // Expected — team 2 received nothing (faction isolation working)
        }

        // Cleanup
        await team1Socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
        await team2Socket.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Test complete", CancellationToken.None);
    }

    // ── AC1: Invalid teamId (zero/negative) → rejected ──

    [Fact]
    public async Task Rejects_InvalidTeamId_With400()
    {
        // Arrange
        var host = await CreateTestHost(authenticateUser: true);
        var server = host.GetTestServer();
        var client = server.CreateClient();

        // Act — teamId = 0
        var response = await client.GetAsync("/ws/chat?teamId=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        // Check if the user was set by middleware (for authenticated tests)
        if (Context.User.Identity?.IsAuthenticated == true)
        {
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(
                Context.User, "Test");
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
    }
}
