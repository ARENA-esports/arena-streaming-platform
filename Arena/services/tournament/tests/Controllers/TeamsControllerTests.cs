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

    private static IFormFile CreateTestFormFile(string fileName, string contentType, long sizeBytes = 1024)
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, sizeBytes, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    #region Get All Teams Tests

    [Fact]
    public async Task GetAllTeams_ExistingTeams_Returns200OKWithTeamsList()
    {
        // Arrange
        var mockTeams = new List<TeamResponse>
        {
            new(1, "Team Crimson", "#FF0055", "https://assets.arena.gg/teams/crimson.png", DateTime.UtcNow, null),
            new(2, "Team Cobalt", "#0077FF", "https://assets.arena.gg/teams/cobalt.png", DateTime.UtcNow, null)
        };

        _mockRepository.Setup(r => r.GetAllTeamsAsync())
            .ReturnsAsync(mockTeams);

        // Act
        var result = await _controller.GetAllTeams();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsAssignableFrom<IReadOnlyList<TeamResponse>>(okResult.Value);
        Assert.Equal(2, response.Count);
        Assert.Equal("Team Crimson", response[0].TeamName);
        Assert.Equal("Team Cobalt", response[1].TeamName);
        _mockRepository.Verify(r => r.GetAllTeamsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllTeams_NoTeams_Returns200OKWithEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllTeamsAsync())
            .ReturnsAsync(new List<TeamResponse>());

        // Act
        var result = await _controller.GetAllTeams();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsAssignableFrom<IReadOnlyList<TeamResponse>>(okResult.Value);
        Assert.Empty(response);
    }

    #endregion

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
    public async Task GetTeamById_WithActiveRosterPlayers_Returns200OKWithPopulatedRoster()
    {
        // Arrange
        const int teamId = 1;
        var existingTeam = new TeamDetailsResponse(
            teamId,
            "Team Crimson",
            "#FF0055",
            LogoUrl: "https://assets.arena.gg/teams/crimson.png",
            Roster: new List<PlayerResponse>
            {
                new(1, teamId, "ViperX", "Captain", true),
                new(2, teamId, "Blaze", "Duelist", true),
                new(3, teamId, "Phantom", "Support", true)
            },
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null
        );

        _mockRepository.Setup(r => r.GetTeamWithRosterAsync(teamId))
            .ReturnsAsync(existingTeam);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<TeamDetailsResponse>(okResult.Value);
        Assert.Equal(teamId, response.TeamId);
        Assert.Equal("Team Crimson", response.TeamName);
        Assert.Equal("#FF0055", response.ColorHex);
        Assert.Equal("https://assets.arena.gg/teams/crimson.png", response.LogoUrl);
        Assert.Equal(3, response.Roster.Count);
        Assert.Equal("ViperX", response.Roster[0].Username);
        Assert.Equal("Captain", response.Roster[0].Role);
        Assert.Equal("Blaze", response.Roster[1].Username);
    }

    [Fact]
    public async Task GetTeamById_WithEmptyRoster_Returns200OKWithEmptyRosterArray()
    {
        // Arrange
        const int teamId = 4;
        var existingTeam = new TeamDetailsResponse(
            teamId,
            "Team Shadow",
            "#8A2BE2",
            LogoUrl: null,
            Roster: new List<PlayerResponse>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null
        );

        _mockRepository.Setup(r => r.GetTeamWithRosterAsync(teamId))
            .ReturnsAsync(existingTeam);

        // Act
        var result = await _controller.GetTeamById(teamId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<TeamDetailsResponse>(okResult.Value);
        Assert.Equal(teamId, response.TeamId);
        Assert.Equal("Team Shadow", response.TeamName);
        Assert.NotNull(response.Roster);
        Assert.Empty(response.Roster);
    }

    [Fact]
    public async Task GetTeamById_NonExistentTeam_Returns404NotFound()
    {
        // Arrange
        const int nonExistentId = 999;
        _mockRepository.Setup(r => r.GetTeamWithRosterAsync(nonExistentId))
            .ReturnsAsync((TeamDetailsResponse?)null);

        // Act
        var result = await _controller.GetTeamById(nonExistentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region Upload Team Logo Tests

    [Fact]
    public async Task UploadTeamLogo_ValidImageFile_Returns200OKWithPublicUrlAndPersistsToDb()
    {
        // Arrange
        const int teamId = 1;
        const string publicUrl = "https://assets.arena.gg/uploads/logos/logo_123.png";
        var existingTeam = new TeamResponse(teamId, "Team Crimson", "#FF0055", LogoUrl: null, CreatedAt: DateTime.UtcNow, UpdatedAt: null);
        var file = CreateTestFormFile("logo.png", "image/png", 500 * 1024);

        _mockRepository.Setup(r => r.GetTeamByIdAsync(teamId))
            .ReturnsAsync(existingTeam);

        string? outError = null;
        _mockFileStorageService.Setup(s => s.ValidateTeamLogo(file, out outError))
            .Returns(true);

        _mockFileStorageService.Setup(s => s.SaveFileAsync(file, "logos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicUrl);

        _mockRepository.Setup(r => r.UpdateTeamLogoAsync(teamId, publicUrl))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadTeamLogo(teamId, file);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var response = Assert.IsType<UploadTeamLogoResponse>(okResult.Value);
        Assert.Equal(teamId, response.TeamId);
        Assert.Equal(publicUrl, response.LogoUrl);

        _mockFileStorageService.Verify(s => s.SaveFileAsync(file, "logos", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateTeamLogoAsync(teamId, publicUrl), Times.Once);
    }

    [Fact]
    public async Task UploadTeamLogo_NonExistentTeam_Returns404NotFound()
    {
        // Arrange
        const int nonExistentTeamId = 888;
        var file = CreateTestFormFile("logo.png", "image/png");

        _mockRepository.Setup(r => r.GetTeamByIdAsync(nonExistentTeamId))
            .ReturnsAsync((TeamResponse?)null);

        // Act
        var result = await _controller.UploadTeamLogo(nonExistentTeamId, file);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);

        _mockFileStorageService.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateTeamLogoAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadTeamLogo_InvalidFile_Returns400BadRequest()
    {
        // Arrange
        const int teamId = 1;
        var existingTeam = new TeamResponse(teamId, "Team Crimson", "#FF0055", LogoUrl: null, CreatedAt: DateTime.UtcNow, UpdatedAt: null);
        var oversizedFile = CreateTestFormFile("large.png", "image/png", 3 * 1024 * 1024);

        _mockRepository.Setup(r => r.GetTeamByIdAsync(teamId))
            .ReturnsAsync(existingTeam);

        string? expectedError = "File size exceeds the maximum allowed limit of 2 MB.";
        _mockFileStorageService.Setup(s => s.ValidateTeamLogo(oversizedFile, out expectedError))
            .Returns(false);

        // Act
        var result = await _controller.UploadTeamLogo(teamId, oversizedFile);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        _mockFileStorageService.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateTeamLogoAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadTeamLogo_DatabaseUpdateFails_Returns500InternalServerError()
    {
        // Arrange
        const int teamId = 1;
        const string publicUrl = "https://assets.arena.gg/uploads/logos/logo.png";
        var existingTeam = new TeamResponse(teamId, "Team Crimson", "#FF0055", LogoUrl: null, CreatedAt: DateTime.UtcNow, UpdatedAt: null);
        var file = CreateTestFormFile("logo.png", "image/png");

        _mockRepository.Setup(r => r.GetTeamByIdAsync(teamId))
            .ReturnsAsync(existingTeam);

        string? outError = null;
        _mockFileStorageService.Setup(s => s.ValidateTeamLogo(file, out outError))
            .Returns(true);

        _mockFileStorageService.Setup(s => s.SaveFileAsync(file, "logos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicUrl);

        _mockRepository.Setup(r => r.UpdateTeamLogoAsync(teamId, publicUrl))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UploadTeamLogo(teamId, file);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    #endregion
}
