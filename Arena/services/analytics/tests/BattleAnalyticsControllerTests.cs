using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using AnalyticsService.Controllers;
using AnalyticsService.DTOs;
using AnalyticsService.Services;
using Xunit;

namespace AnalyticsService.Tests;

public class BattleAnalyticsControllerTests
{
    private readonly Mock<IBattleStatsService> _serviceMock;
    private readonly Mock<ILogger<BattleAnalyticsController>> _loggerMock;
    private readonly BattleAnalyticsController _controller;

    public BattleAnalyticsControllerTests()
    {
        _serviceMock = new Mock<IBattleStatsService>();
        _loggerMock = new Mock<ILogger<BattleAnalyticsController>>();
        _controller = new BattleAnalyticsController(_serviceMock.Object, _loggerMock.Object);

        SetUserContext("Organizer");
    }

    private void SetUserContext(string role)
    {
        var claims = new List<Claim>
        {
            new("sub", "99"),
            new(ClaimTypes.Role, role),
            new("unique_name", "TestOrganizer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetAllBattleSummaries_AsOrganizer_Returns200WithSummaries()
    {
        // Arrange
        var summaries = new List<StreamTeamBattleResponse>
        {
            new()
            {
                StreamId = 101,
                TeamId = 1,
                TeamName = "Team Crimson",
                TotalAttacks = 150,
                TotalDamageDealt = 4500L,
                TotalCoinsSpent = 12000L,
                RoundsWon = 3,
                RoundsLost = 1
            }
        };

        _serviceMock
            .Setup(s => s.GetAllBattleSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        // Act
        var result = await _controller.GetAllBattleSummaries(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var list = Assert.IsAssignableFrom<IEnumerable<StreamTeamBattleResponse>>(okResult.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Team Crimson", list[0].TeamName);
        _serviceMock.Verify(s => s.GetAllBattleSummariesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllBattleSummaries_WhenEmpty_Returns200WithEmptyCollection()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetAllBattleSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StreamTeamBattleResponse>());

        // Act
        var result = await _controller.GetAllBattleSummaries(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var list = Assert.IsAssignableFrom<IEnumerable<StreamTeamBattleResponse>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetStreamBattleDashboard_AsOrganizer_Returns200WithCombinedDashboard()
    {
        // Arrange
        var streamId = 101;
        var dashboard = new StreamBattleDashboardResponse
        {
            StreamId = streamId,
            Teams = new List<StreamTeamBattleResponse>
            {
                new() { StreamId = streamId, TeamId = 1, TeamName = "Team Crimson", TotalAttacks = 50 }
            },
            Rounds = new List<StreamRoundOutcomeResponse>
            {
                new() { StreamId = streamId, RoundNumber = 1, WinningTeamId = 1, WinningTeamName = "Team Crimson" }
            }
        };

        _serviceMock
            .Setup(s => s.GetStreamBattleDashboardAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _controller.GetStreamBattleDashboard(streamId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var response = Assert.IsType<StreamBattleDashboardResponse>(okResult.Value);
        Assert.Equal(streamId, response.StreamId);
        Assert.Single(response.Teams);
        Assert.Single(response.Rounds);
        _serviceMock.Verify(s => s.GetStreamBattleDashboardAsync(streamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStreamBattleDashboard_WhenEmpty_Returns200WithEmptyDashboard()
    {
        // Arrange
        var streamId = 999;
        var dashboard = new StreamBattleDashboardResponse
        {
            StreamId = streamId,
            Teams = new List<StreamTeamBattleResponse>(),
            Rounds = new List<StreamRoundOutcomeResponse>()
        };

        _serviceMock
            .Setup(s => s.GetStreamBattleDashboardAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        // Act
        var result = await _controller.GetStreamBattleDashboard(streamId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var response = Assert.IsType<StreamBattleDashboardResponse>(okResult.Value);
        Assert.Equal(streamId, response.StreamId);
        Assert.Empty(response.Teams);
        Assert.Empty(response.Rounds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetStreamBattleDashboard_InvalidStreamId_Returns400BadRequest(int invalidStreamId)
    {
        // Act
        var result = await _controller.GetStreamBattleDashboard(invalidStreamId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _serviceMock.Verify(s => s.GetStreamBattleDashboardAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRoundOutcomesByStreamId_AsOrganizer_Returns200WithRounds()
    {
        // Arrange
        var streamId = 101;
        var rounds = new List<StreamRoundOutcomeResponse>
        {
            new() { StreamId = streamId, RoundNumber = 1, WinningTeamId = 1, WinningTeamName = "Team Crimson" },
            new() { StreamId = streamId, RoundNumber = 2, WinningTeamId = 2, WinningTeamName = "Team Azure" }
        };

        _serviceMock
            .Setup(s => s.GetRoundOutcomesByStreamIdAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rounds);

        // Act
        var result = await _controller.GetRoundOutcomesByStreamId(streamId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var list = Assert.IsAssignableFrom<IEnumerable<StreamRoundOutcomeResponse>>(okResult.Value).ToList();
        Assert.Equal(2, list.Count);
        _serviceMock.Verify(s => s.GetRoundOutcomesByStreamIdAsync(streamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRoundOutcomesByStreamId_WhenEmpty_Returns200WithEmptyCollection()
    {
        // Arrange
        var streamId = 202;
        _serviceMock
            .Setup(s => s.GetRoundOutcomesByStreamIdAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StreamRoundOutcomeResponse>());

        // Act
        var result = await _controller.GetRoundOutcomesByStreamId(streamId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var list = Assert.IsAssignableFrom<IEnumerable<StreamRoundOutcomeResponse>>(okResult.Value);
        Assert.Empty(list);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetRoundOutcomesByStreamId_InvalidStreamId_Returns400BadRequest(int invalidStreamId)
    {
        // Act
        var result = await _controller.GetRoundOutcomesByStreamId(invalidStreamId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _serviceMock.Verify(s => s.GetRoundOutcomesByStreamIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Controller_HasOrganizerAuthorizeAttribute()
    {
        // Assert Authorize attribute is present on the controller class with Organizer role
        var controllerAuthAttr = typeof(BattleAnalyticsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(controllerAuthAttr);
        Assert.Equal("Organizer", controllerAuthAttr.Roles);

        // Assert Route attribute
        var routeAttr = typeof(BattleAnalyticsController).GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttr);
        Assert.Equal("api/analytics/battle", routeAttr.Template);

        // Assert action method HTTP attributes
        var getAllMethod = typeof(BattleAnalyticsController).GetMethod(nameof(BattleAnalyticsController.GetAllBattleSummaries));
        Assert.NotNull(getAllMethod);
        var getAllAttr = getAllMethod.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(getAllAttr);
        Assert.Equal("streams", getAllAttr.Template);

        var getDashboardMethod = typeof(BattleAnalyticsController).GetMethod(nameof(BattleAnalyticsController.GetStreamBattleDashboard));
        Assert.NotNull(getDashboardMethod);
        var getDashboardAttr = getDashboardMethod.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(getDashboardAttr);
        Assert.Equal("streams/{streamId:int}", getDashboardAttr.Template);

        var getRoundsMethod = typeof(BattleAnalyticsController).GetMethod(nameof(BattleAnalyticsController.GetRoundOutcomesByStreamId));
        Assert.NotNull(getRoundsMethod);
        var getRoundsAttr = getRoundsMethod.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(getRoundsAttr);
        Assert.Equal("streams/{streamId:int}/rounds", getRoundsAttr.Template);
    }

    [Fact]
    public async Task Controller_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Service processing failure");
        _serviceMock
            .Setup(s => s.GetAllBattleSummariesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetAllBattleSummaries(CancellationToken.None));

        Assert.Same(expectedException, actualException);
    }
}
