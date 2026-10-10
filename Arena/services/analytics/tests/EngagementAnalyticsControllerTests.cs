using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using AnalyticsService.Controllers;
using AnalyticsService.DTOs;
using AnalyticsService.Entities;
using AnalyticsService.Repositories;
using Xunit;

namespace AnalyticsService.Tests;

public class EngagementAnalyticsControllerTests
{
    private readonly Mock<IAnalyticsRepository> _repositoryMock;
    private readonly Mock<ILogger<EngagementAnalyticsController>> _loggerMock;
    private readonly EngagementAnalyticsController _controller;

    public EngagementAnalyticsControllerTests()
    {
        _repositoryMock = new Mock<IAnalyticsRepository>();
        _loggerMock = new Mock<ILogger<EngagementAnalyticsController>>();
        _controller = new EngagementAnalyticsController(_repositoryMock.Object, _loggerMock.Object);

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
    public async Task GetStreamEngagement_AsOrganizer_Returns200WithEngagementSummaries()
    {
        // Arrange
        var timestamp1 = new DateTime(2026, 10, 5, 10, 30, 0, DateTimeKind.Utc);
        var timestamp2 = new DateTime(2026, 10, 5, 11, 15, 0, DateTimeKind.Utc);

        var summaries = new List<StreamEngagementSummary>
        {
            new()
            {
                StreamId = 101,
                TotalWatchSeconds = 7200,
                TotalCoinsEarned = 1200,
                TotalWatchTicks = 120,
                UniqueViewers = 25,
                LastEventAt = timestamp1,
                CreatedAt = timestamp1.AddHours(-2),
                UpdatedAt = timestamp1
            },
            new()
            {
                StreamId = 202,
                TotalWatchSeconds = 3600,
                TotalCoinsEarned = 600,
                TotalWatchTicks = 60,
                UniqueViewers = 10,
                LastEventAt = timestamp2,
                CreatedAt = timestamp2.AddHours(-1),
                UpdatedAt = timestamp2
            }
        };

        _repositoryMock
            .Setup(r => r.GetAllEngagementSummariesAsync())
            .ReturnsAsync(summaries);

        // Act
        var result = await _controller.GetStreamEngagement();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var responseList = Assert.IsAssignableFrom<IEnumerable<StreamEngagementResponse>>(okResult.Value).ToList();
        Assert.Equal(2, responseList.Count);

        var first = responseList[0];
        Assert.Equal(101, first.StreamId);
        Assert.Equal(7200, first.TotalWatchSeconds);
        Assert.Equal(1200, first.TotalCoinsEarned);
        Assert.Equal(120, first.TotalWatchTicks);
        Assert.Equal(25, first.UniqueViewers);
        Assert.Equal(timestamp1, first.LastEventAt);

        var second = responseList[1];
        Assert.Equal(202, second.StreamId);
        Assert.Equal(3600, second.TotalWatchSeconds);
        Assert.Equal(600, second.TotalCoinsEarned);
        Assert.Equal(60, second.TotalWatchTicks);
        Assert.Equal(10, second.UniqueViewers);
        Assert.Equal(timestamp2, second.LastEventAt);

        _repositoryMock.Verify(r => r.GetAllEngagementSummariesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStreamEngagement_WhenEmpty_Returns200WithEmptyCollection()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllEngagementSummariesAsync())
            .ReturnsAsync(new List<StreamEngagementSummary>());

        // Act
        var result = await _controller.GetStreamEngagement();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var responseList = Assert.IsAssignableFrom<IEnumerable<StreamEngagementResponse>>(okResult.Value);
        Assert.Empty(responseList);

        _repositoryMock.Verify(r => r.GetAllEngagementSummariesAsync(), Times.Once);
    }

    [Fact]
    public void GetStreamEngagement_Controller_HasOrganizerAuthorizeAttribute()
    {
        // Assert Authorize attribute is present on the controller class
        var controllerAuthAttr = typeof(EngagementAnalyticsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(controllerAuthAttr);
        Assert.Equal("Organizer", controllerAuthAttr.Roles);

        // Assert HTTP Method attribute on the action method
        var method = typeof(EngagementAnalyticsController).GetMethod(nameof(EngagementAnalyticsController.GetStreamEngagement));
        Assert.NotNull(method);

        var httpGetAttr = method.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGetAttr);
        Assert.Equal("streams", httpGetAttr.Template);
    }

    [Fact]
    public async Task GetStreamEngagement_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failure");
        _repositoryMock
            .Setup(r => r.GetAllEngagementSummariesAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetStreamEngagement());

        Assert.Same(expectedException, actualException);
        _repositoryMock.Verify(r => r.GetAllEngagementSummariesAsync(), Times.Once);
    }
}
