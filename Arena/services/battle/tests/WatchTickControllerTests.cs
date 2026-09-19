using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using BattleEconomyService.Controllers;
using BattleEconomyService.DTOs;
using BattleEconomyService.Services;

namespace BattleEconomyService.Tests;

public class WatchTickControllerTests
{
    private readonly Mock<IWatchTickService> _serviceMock;
    private readonly Mock<ILogger<WatchTickController>> _loggerMock;

    public WatchTickControllerTests()
    {
        _serviceMock = new Mock<IWatchTickService>();
        _loggerMock = new Mock<ILogger<WatchTickController>>();
    }

    private WatchTickController CreateController(ClaimsPrincipal? user = null)
    {
        var controller = new WatchTickController(_serviceMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "123")
                    }, "TestAuth"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task AwardWatchTick_Success_Returns200WithUpdatedBalance()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _serviceMock.Setup(s => s.ProcessWatchTickAsync(123, 101))
            .ReturnsAsync(WatchTickResult.Awarded(10, 20, now));

        var controller = CreateController();

        // Act
        var actionResult = await controller.AwardWatchTick(new WatchTickRequest { StreamId = 101 });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<WatchTickResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(10, response.CoinsAwarded);
        Assert.Equal(20, response.CurrentBalance);
        Assert.Equal(now, response.LastTickAt);
    }

    [Fact]
    public async Task AwardWatchTick_RateLimited_Returns429WithRemainingSeconds()
    {
        // Arrange
        var lastTick = DateTime.UtcNow.AddSeconds(-20);
        _serviceMock.Setup(s => s.ProcessWatchTickAsync(123, null))
            .ReturnsAsync(WatchTickResult.TooEarly(10, lastTick, 35));

        var controller = CreateController();

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, statusResult.StatusCode);

        var response = Assert.IsType<WatchTickResponse>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Equal(0, response.CoinsAwarded);
        Assert.Equal(35, response.RemainingSeconds);
        Assert.Contains("Anti-farm", response.Message);
        Assert.Equal("35", controller.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task AwardWatchTick_MissingOrInvalidUserIdClaim_Returns401Unauthorized()
    {
        // Arrange: User with no NameIdentifier / sub claim
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var controller = CreateController(anonymousUser);

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }
}
