/*
    unit tests for MatchesController verifying authorization, input validation,
    business rules (identical teams, past schedules), and public lookups
*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StreamService.Controllers;
using StreamService.DTOs;
using StreamService.Repositories;
using Xunit;
using System.Security.Claims;

namespace StreamService.Tests.Controllers;

public class MatchesControllerTests
{
    private readonly Mock<IMatchRepository> _matchRepoMock;
    private readonly MatchesController _controller;

    public MatchesControllerTests()
    {
        _matchRepoMock = new Mock<IMatchRepository>();
        _controller = new MatchesController(_matchRepoMock.Object);
    }

    /* Helper to configure mock user claims on the controller context */
    private void SetUserContext(string? userId, string role)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.Add(new Claim("sub", userId));
        }
        claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /* ---------------- CreateMatch Tests ---------------- */

    [Fact]
    public async Task CreateMatch_WithIdenticalTeams_Returns400BadRequest()
    {
        // Arrange
        SetUserContext("1", "Organizer");
        var request = new CreateMatchRequest
        {
            TournamentId = 1,
            TeamAId = 10,
            TeamBId = 10, // identical team
            ScheduledTime = DateTimeOffset.UtcNow.AddHours(2)
        };

        // Act
        var result = await _controller.CreateMatch(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateMatch_WithPastScheduledTime_Returns400BadRequest()
    {
        // Arrange
        SetUserContext("1", "Organizer");
        var request = new CreateMatchRequest
        {
            TournamentId = 1,
            TeamAId = 10,
            TeamBId = 20,
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-10) // past-dated (beyond 5 min buffer)
        };

        // Act
        var result = await _controller.CreateMatch(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateMatch_WhenTeamsDoNotExist_Returns400BadRequest()
    {
        // Arrange
        SetUserContext("1", "Organizer");
        var request = new CreateMatchRequest
        {
            TournamentId = 1,
            TeamAId = 99,
            TeamBId = 100,
            ScheduledTime = DateTimeOffset.UtcNow.AddHours(2)
        };

        _matchRepoMock.Setup(m => m.BothTeamsExistAsync(request.TeamAId, request.TeamBId))
            .ReturnsAsync(false); // Teams don't exist

        // Act
        var result = await _controller.CreateMatch(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateMatch_WhenValid_Returns201Created()
    {
        // Arrange
        SetUserContext("1", "Organizer");
        var request = new CreateMatchRequest
        {
            TournamentId = 1,
            TeamAId = 10,
            TeamBId = 20,
            ScheduledTime = DateTimeOffset.UtcNow.AddHours(2)
        };

        const int generatedMatchId = 42;
        _matchRepoMock.Setup(m => m.BothTeamsExistAsync(request.TeamAId, request.TeamBId))
            .ReturnsAsync(true);
        _matchRepoMock.Setup(m => m.CreateMatchAsync(request.TournamentId, request.TeamAId, request.TeamBId, request.ScheduledTime))
            .ReturnsAsync(generatedMatchId);
        
        var expectedMatch = new MatchResponse(generatedMatchId, request.TournamentId, request.TeamAId, request.TeamBId, request.ScheduledTime, "Scheduled", null, DateTime.UtcNow);
        _matchRepoMock.Setup(m => m.GetMatchByIdAsync(generatedMatchId)).ReturnsAsync(expectedMatch);

        // Act
        var result = await _controller.CreateMatch(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(MatchesController.GetMatchById), createdResult.ActionName);
        Assert.Equal(expectedMatch, createdResult.Value);
    }

    /* ---------------- GetMatches Tests (Story 98) ---------------- */

    [Fact]
    public async Task GetMatches_WithNoParameters_PassesDefaultStatusAndNoTeamId()
    {
        // Arrange
        var mockResults = new List<MatchScheduleResponse>
        {
            new MatchScheduleResponse(1, 1, DateTimeOffset.UtcNow, "Scheduled", 
                new TeamSummary(1, "A", "#FFF", null), new TeamSummary(2, "B", "#000", null))
        };
        _matchRepoMock.Setup(m => m.GetAllMatchesAsync(null, null)).ReturnsAsync(mockResults);

        // Act
        var result = await _controller.GetMatches(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(mockResults, okResult.Value);
        _matchRepoMock.Verify(m => m.GetAllMatchesAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetMatches_WithTeamIdAndStatus_PassesFiltersCorrectly()
    {
        // Arrange
        var mockResults = new List<MatchScheduleResponse>(); // Empty result scenario
        _matchRepoMock.Setup(m => m.GetAllMatchesAsync("5", "Live")).ReturnsAsync(mockResults);

        // Act
        var result = await _controller.GetMatches("5", "Live");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(mockResults, okResult.Value);
        _matchRepoMock.Verify(m => m.GetAllMatchesAsync("5", "Live"), Times.Once);
    }

    [Fact]
    public async Task GetMatches_TeamDataCompleteness_ReturnsNestedTeamSummaries()
    {
        // Arrange
        var mockResults = new List<MatchScheduleResponse>
        {
            new MatchScheduleResponse(10, 2, DateTimeOffset.UtcNow, "Ended", 
                new TeamSummary(10, "Team A Name", "#111111", "https://logo.a"), 
                new TeamSummary(20, "Team B Name", "#222222", null))
        };
        _matchRepoMock.Setup(m => m.GetAllMatchesAsync(null, "Ended")).ReturnsAsync(mockResults);

        // Act
        var result = await _controller.GetMatches(null, "Ended");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMatches = Assert.IsAssignableFrom<List<MatchScheduleResponse>>(okResult.Value);
        Assert.Single(returnedMatches);
        var match = returnedMatches[0];
        
        // Assert completeness of nested data
        Assert.Equal(10, match.TeamA.TeamId);
        Assert.Equal("Team A Name", match.TeamA.name);
        Assert.Equal("https://logo.a", match.TeamA.LogoUrl);
        Assert.Null(match.TeamB.LogoUrl);
    }

    /* ---------------- GetMatchById Tests ---------------- */

    [Fact]
    public async Task GetMatchById_WhenExists_Returns200OK()
    {
        // Arrange
        const int matchId = 10;
        var expectedMatch = new MatchResponse(matchId, 1, 100, 200, DateTimeOffset.UtcNow, "Scheduled", null, DateTime.UtcNow);
        _matchRepoMock.Setup(m => m.GetMatchByIdAsync(matchId)).ReturnsAsync(expectedMatch);

        // Act
        var result = await _controller.GetMatchById(matchId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedMatch, okResult.Value);
    }

    [Fact]
    public async Task GetMatchById_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        _matchRepoMock.Setup(m => m.GetMatchByIdAsync(999)).ReturnsAsync((MatchResponse?)null);

        // Act
        var result = await _controller.GetMatchById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }
}
