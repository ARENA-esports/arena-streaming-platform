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

    // =========================================================================
    // Test C (Controller level): 54-second rejection returns 429
    // =========================================================================
    [Fact]
    public async Task AwardWatchTick_RateLimitedAt54Seconds_Returns429WithRemainingSecondsAndRetryAfterHeader()
    {
        // Arrange
        var lastTick = DateTime.UtcNow.AddSeconds(-54);
        _serviceMock.Setup(s => s.ProcessWatchTickAsync(123, null))
            .ReturnsAsync(WatchTickResult.TooEarly(10, lastTick, 1));

        var controller = CreateController();

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, statusResult.StatusCode);

        var response = Assert.IsType<WatchTickResponse>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Equal(0, response.CoinsAwarded);
        Assert.Equal(1, response.RemainingSeconds);
        Assert.Equal("1", controller.Response.Headers["Retry-After"].ToString());
    }

    // =========================================================================
    // Test I: Authentication tests (missing, malformed, negative, or invalid stream)
    // =========================================================================
    [Fact]
    public async Task AwardWatchTick_MissingUserIdentityClaim_Returns401Unauthorized()
    {
        // Arrange: Empty identity
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var controller = CreateController(anonymousUser);

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task AwardWatchTick_MalformedNonNumericUserIdClaim_Returns401Unauthorized()
    {
        // Arrange: Malformed sub/NameIdentifier
        var malformedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-number")
        }, "TestAuth"));
        var controller = CreateController(malformedUser);

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task AwardWatchTick_NonPositiveUserIdClaim_Returns401Unauthorized()
    {
        // Arrange: Negative user ID
        var negativeUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "-1")
        }, "TestAuth"));
        var controller = CreateController(negativeUser);

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult);
    }

    [Fact]
    public async Task AwardWatchTick_InvalidNegativeStreamId_Returns400BadRequest()
    {
        // Arrange: Valid user, but invalid streamId <= 0
        var controller = CreateController();

        // Act
        var actionResult = await controller.AwardWatchTick(new WatchTickRequest { StreamId = -5 });

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    // =========================================================================
    // SCRUM-115 Controller Tests: CapExceeded -> HTTP 429
    // =========================================================================

    [Fact]
    public async Task AwardWatchTick_CapExceeded_Returns429TooManyRequests()
    {
        // Arrange
        _serviceMock.Setup(s => s.ProcessWatchTickAsync(123, 101))
            .ReturnsAsync(new WatchTickResult
            {
                Status = WatchTickStatus.CapExceeded,
                CoinsAwarded = 0,
                CurrentBalance = 50,
                Message = "Coin cap reached for this stream window."
            });

        var controller = CreateController();

        // Act
        var actionResult = await controller.AwardWatchTick(new WatchTickRequest { StreamId = 101 });

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, statusResult.StatusCode);

        var response = Assert.IsType<WatchTickResponse>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Equal(0, response.CoinsAwarded);
        Assert.Equal(50, response.CurrentBalance);
    }

    [Fact]
    public async Task AwardWatchTick_CapExceeded_ResponseHasZeroCoins()
    {
        // Arrange
        _serviceMock.Setup(s => s.ProcessWatchTickAsync(123, null))
            .ReturnsAsync(new WatchTickResult
            {
                Status = WatchTickStatus.CapExceeded,
                CoinsAwarded = 0,
                CurrentBalance = 50,
                Message = "Coin cap reached for this stream window."
            });

        var controller = CreateController();

        // Act
        var actionResult = await controller.AwardWatchTick(null);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, statusResult.StatusCode);

        var response = Assert.IsType<WatchTickResponse>(statusResult.Value);
        Assert.Equal(0, response.CoinsAwarded);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task AwardWatchTick_RateLimited_StillReturns429WithRetryAfterHeader()
    {
        // Verify existing SCRUM-114 RateLimited 429 behavior is unaffected
        var lastTick = DateTime.UtcNow.AddSeconds(-20);
        _serviceMock.Setup(s => s.ProcessWatchTickAsync(123, null))
            .ReturnsAsync(WatchTickResult.TooEarly(10, lastTick, 35));

        var controller = CreateController();
        var actionResult = await controller.AwardWatchTick(null);

        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, statusResult.StatusCode);

        var response = Assert.IsType<WatchTickResponse>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Equal(0, response.CoinsAwarded);
        Assert.Equal(35, response.RemainingSeconds);
        Assert.Equal("35", controller.Response.Headers["Retry-After"].ToString());
    }
}
