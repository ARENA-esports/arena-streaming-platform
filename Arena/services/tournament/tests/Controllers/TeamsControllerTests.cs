using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TournamentService.Controllers;
using TournamentService.DTOs;
using TournamentService.Exceptions;
using TournamentService.Repositories;
using TournamentService.Services;
using Xunit;

namespace TournamentService.Tests.Controllers;

public class TeamsControllerTests
{
    private readonly Mock<ITeamRepository> _mockRepository;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<ILogger<TeamsController>> _mockLogger;
    private readonly TeamsController _controller;

    public TeamsControllerTests()
    {
        _mockRepository = new Mock<ITeamRepository>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<TeamsController>>();
        _controller = new TeamsController(
            _mockRepository.Object,
            _mockFileStorageService.Object,
            _mockLogger.Object
        );
        SetUserContext("Organizer");
    }

    private void SetUserContext(string role)
    {
        var claims = new List<Claim>
        {
            new("sub", "201"),
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

    #region Create Team Tests

    [Fact]
    public async Task CreateTeam_ValidPayload_Returns201CreatedWithAssignedTeamId()
    {
        // Arrange
        const int assignedId = 1;
        var request = new CreateTeamRequest
        {
            TeamName = "Team Crimson",
            ColorHex = "#FF0055"
        };

        var createdTeam = new TeamResponse(
            assignedId,
            request.TeamName,
            request.ColorHex,
            LogoUrl: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null
        );

        _mockRepository.Setup(r => r.CreateTeamAsync("Team Crimson", "#FF0055"))
            .ReturnsAsync(assignedId);

        _mockRepository.Setup(r => r.GetTeamByIdAsync(assignedId))
            .ReturnsAsync(createdTeam);

        // Act
        var result = await _controller.CreateTeam(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
        Assert.Equal(assignedId, createdAtActionResult.RouteValues?["id"]);

        var response = Assert.IsType<TeamResponse>(createdAtActionResult.Value);
        Assert.Equal(assignedId, response.TeamId);
        Assert.Equal("Team Crimson", response.TeamName);
        Assert.Equal("#FF0055", response.ColorHex);
        _mockRepository.Verify(r => r.CreateTeamAsync("Team Crimson", "#FF0055"), Times.Once);
    }

    [Theory]
    [InlineData("FF0055")]    // Missing #
    [InlineData("#FFF")]       // Too short
    [InlineData("#GG0055")]    // Non-hex character
    [InlineData("#FF0055AA")]  // Too long
    [InlineData("#12345")]     // 6 characters
    [InlineData("not-a-color")]
    public async Task CreateTeam_InvalidHexFormat_Returns400BadRequest(string invalidHex)
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = "Team Cobalt",
            ColorHex = invalidHex
        };

        // Act
        var result = await _controller.CreateTeam(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CreateTeamAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateTeam_WhitespaceOrMissingTeamName_Returns400BadRequest(string? invalidName)
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = invalidName!,
            ColorHex = "#0077FF"
        };

        // Act
        var result = await _controller.CreateTeam(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CreateTeamAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateTeam_InvalidModelState_Returns400BadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("ColorHex", "Invalid hex format.");
        var request = new CreateTeamRequest();

        // Act
        var result = await _controller.CreateTeam(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CreateTeamAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateTeam_DuplicateTeamName_Returns409Conflict()
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = "Team Crimson",
            ColorHex = "#FF0055"
        };

        _mockRepository.Setup(r => r.CreateTeamAsync("Team Crimson", "#FF0055"))
            .ThrowsAsync(new TeamConflictException("Team Crimson"));

        // Act
        var result = await _controller.CreateTeam(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        _mockRepository.Verify(r => r.CreateTeamAsync("Team Crimson", "#FF0055"), Times.Once);
    }

    #endregion

    #region Get Team By Id Tests

    [Fact]
    public async Task GetTeamById_ExistingTeam_Returns200OK()
    {
        // Arrange
        const int teamId = 1;
        var existingTeam = new TeamResponse(teamId, "Team Crimson", "#FF0055", LogoUrl: null, CreatedAt: DateTime.UtcNow, UpdatedAt: null);

        _mockRepository.Setup(r => r.GetTeamByIdAsync(teamId))
            .ReturnsAsync(existingTeam);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<TeamResponse>(okResult.Value);
        Assert.Equal(teamId, response.TeamId);
        Assert.Equal("Team Crimson", response.TeamName);
        Assert.Equal("#FF0055", response.ColorHex);
    }

    [Fact]
    public async Task GetTeamById_NonExistentTeam_Returns404NotFound()
    {
        // Arrange
        const int nonExistentId = 999;
        _mockRepository.Setup(r => r.GetTeamByIdAsync(nonExistentId))
            .ReturnsAsync((TeamResponse?)null);

        // Act
        var result = await _controller.GetTeamById(nonExistentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion
}
