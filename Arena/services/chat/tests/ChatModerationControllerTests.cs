using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ChatService.Controllers;
using ChatService.Entities;
using ChatService.Models;
using ChatService.Repositories;
using Xunit;

namespace ChatService.Tests;

public class ChatModerationControllerTests
{
    private readonly Mock<IChatMuteRepository> _muteRepoMock;
    private readonly Mock<ILogger<ChatModerationController>> _loggerMock;
    private readonly ChatModerationController _controller;

    public ChatModerationControllerTests()
    {
        _muteRepoMock = new Mock<IChatMuteRepository>();
        _loggerMock = new Mock<ILogger<ChatModerationController>>();
        _controller = new ChatModerationController(_muteRepoMock.Object, _loggerMock.Object);
    }

    private void SetUser(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task MuteUser_AsOrganizer_Returns200()
    {
        SetUser(100, "Organizer");
        _muteRepoMock.Setup(r => r.InsertAsync(It.IsAny<ChatMute>())).ReturnsAsync(1L);

        var request = new MuteUserRequest { UserId = 42, DurationMinutes = 30, Reason = "Spam" };
        var result = await _controller.MuteUser(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _muteRepoMock.Verify(r => r.InsertAsync(It.Is<ChatMute>(m =>
            m.UserId == 42 && m.MutedBy == 100 && m.Reason == "Spam")), Times.Once);
    }

    [Fact]
    public async Task MuteUser_AsAdmin_Returns200()
    {
        SetUser(1, "Admin");
        _muteRepoMock.Setup(r => r.InsertAsync(It.IsAny<ChatMute>())).ReturnsAsync(2L);

        var request = new MuteUserRequest { UserId = 42, DurationMinutes = 60 };
        var result = await _controller.MuteUser(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task MuteUser_InvalidModelState_Returns400()
    {
        SetUser(100, "Organizer");
        _controller.ModelState.AddModelError("UserId", "Required");

        var request = new MuteUserRequest { UserId = 0, DurationMinutes = 30 };
        var result = await _controller.MuteUser(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task MuteUser_NoUserIdInToken_ReturnsUnauthorized()
    {
        // Set a user with no NameIdentifier claim
        var claims = new List<Claim> { new(ClaimTypes.Role, "Organizer") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var request = new MuteUserRequest { UserId = 42, DurationMinutes = 30 };
        var result = await _controller.MuteUser(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task MuteUser_RepositoryThrows_Returns500()
    {
        SetUser(100, "Organizer");
        _muteRepoMock.Setup(r => r.InsertAsync(It.IsAny<ChatMute>()))
            .ThrowsAsync(new Exception("DB down"));

        var request = new MuteUserRequest { UserId = 42, DurationMinutes = 30 };
        var result = await _controller.MuteUser(request);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetMuteStatus_UserNotMuted_ReturnsNotMuted()
    {
        SetUser(100, "Organizer");
        _muteRepoMock.Setup(r => r.GetActiveMuteAsync(42)).ReturnsAsync((ChatMute?)null);

        var result = await _controller.GetMuteStatus(42);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMuteStatus_UserMuted_ReturnsMuteDetails()
    {
        SetUser(100, "Organizer");
        _muteRepoMock.Setup(r => r.GetActiveMuteAsync(42)).ReturnsAsync(new ChatMute
        {
            MuteId = 1,
            UserId = 42,
            MutedBy = 100,
            Reason = "Spam",
            MutedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });

        var result = await _controller.GetMuteStatus(42);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMuteStatus_InvalidUserId_Returns400()
    {
        SetUser(100, "Organizer");

        var result = await _controller.GetMuteStatus(0);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
