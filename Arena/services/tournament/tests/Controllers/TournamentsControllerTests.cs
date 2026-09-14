using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TournamentService.Controllers;
using TournamentService.DTOs;
using TournamentService.Models;
using TournamentService.Repositories;
using Xunit;

namespace TournamentService.Tests.Controllers;

public class TournamentsControllerTests
{
    private readonly Mock<ITournamentRepository> _mockRepository;
    private readonly Mock<ILogger<TournamentsController>> _mockLogger;
    private readonly TournamentsController _controller;

    public TournamentsControllerTests()
    {
        _mockRepository = new Mock<ITournamentRepository>();
        _mockLogger = new Mock<ILogger<TournamentsController>>();
        _controller = new TournamentsController(_mockRepository.Object, _mockLogger.Object);
        SetUserContext("Organizer");
    }

    private void SetUserContext(string role)
    {
        var claims = new List<Claim>
        {
            new("sub", "101"),
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

    #region Create Tournament Tests

    [Fact]
    public async Task CreateTournament_ValidRequest_Returns201CreatedWithAssignedId()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(7);
        var endDate = startDate.AddDays(14);
        var request = new CreateTournamentRequest
        {
            Name = "Championship Series 2026",
            SeasonIdentifier = "SEASON-2026-Q1",
            StartDate = startDate,
            EndDate = endDate
        };

        const int assignedId = 42;
        var createdResponse = new TournamentResponse(
            assignedId,
            request.Name,
            request.SeasonIdentifier,
            request.StartDate,
            request.EndDate,
            TournamentStatus.Scheduled,
            DateTime.UtcNow,
            null
        );

        _mockRepository.Setup(r => r.CreateTournamentAsync(
            request.Name,
            request.SeasonIdentifier,
            request.StartDate,
            request.EndDate
        )).ReturnsAsync(assignedId);

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(assignedId))
            .ReturnsAsync(createdResponse);

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdAtActionResult.StatusCode);
        Assert.Equal(assignedId, createdAtActionResult.RouteValues?["id"]);

        var response = Assert.IsType<TournamentResponse>(createdAtActionResult.Value);
        Assert.Equal(assignedId, response.Id);
        Assert.Equal("Championship Series 2026", response.Name);
        Assert.Equal("SEASON-2026-Q1", response.SeasonIdentifier);
        Assert.Equal(TournamentStatus.Scheduled, response.Status);
    }

    [Fact]
    public async Task CreateTournament_EndDateBeforeStartDate_Returns400BadRequest()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(7);
        var endDate = startDate.AddDays(-1); // Invalid: EndDate < StartDate
        var request = new CreateTournamentRequest
        {
            Name = "Invalid Date Range Tournament",
            SeasonIdentifier = "SEASON-2026-Q1",
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CreateTournamentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task CreateTournament_WhitespaceName_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Name = "   ",
            SeasonIdentifier = "SEASON-2026-Q1",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(5)
        };

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CreateTournamentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task CreateTournament_WhitespaceSeasonIdentifier_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Name = "Summer Major",
            SeasonIdentifier = "   ",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(5)
        };

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateTournament_InvalidModelState_Returns400BadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Name is required.");
        var request = new CreateTournamentRequest();

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    #region View Tournament Details Tests

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.Active)]
    [InlineData(TournamentStatus.Completed)]
    [InlineData(TournamentStatus.Cancelled)]
    public async Task GetTournamentById_ExistingTournament_Returns200WithCurrentStatus(string status)
    {
        // Arrange
        const int tournamentId = 5;
        var tournament = new TournamentResponse(
            tournamentId,
            "Arena Pro Circuit",
            "APC-2026",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            status
        );

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(tournament);

        // Act
        var result = await _controller.GetTournamentById(tournamentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<TournamentResponse>(okResult.Value);
        Assert.Equal(tournamentId, response.Id);
        Assert.Equal(status, response.Status);
    }

    [Fact]
    public async Task GetTournamentById_NonExistentTournament_Returns404NotFound()
    {
        // Arrange
        const int nonExistentId = 999;
        _mockRepository.Setup(r => r.GetTournamentByIdAsync(nonExistentId))
            .ReturnsAsync((TournamentResponse?)null);

        // Act
        var result = await _controller.GetTournamentById(nonExistentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetAllTournaments_Returns200WithList()
    {
        // Arrange
        var tournaments = new List<TournamentResponse>
        {
            new(1, "Winter Open", "WINTER-2026", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10), TournamentStatus.Completed),
            new(2, "Spring Open", "SPRING-2026", DateTimeOffset.UtcNow.AddDays(15), DateTimeOffset.UtcNow.AddDays(25), TournamentStatus.Scheduled)
        };

        _mockRepository.Setup(r => r.GetAllTournamentsAsync())
            .ReturnsAsync(tournaments);

        // Act
        var result = await _controller.GetAllTournaments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsAssignableFrom<IEnumerable<TournamentResponse>>(okResult.Value);
        Assert.Equal(2, response.Count());
    }

    #endregion

    #region Update Tournament Tests

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.Active)]
    public async Task UpdateTournament_WhenStatusAllowsModifications_Returns200OK(string currentStatus)
    {
        // Arrange
        const int tournamentId = 10;
        var existing = new TournamentResponse(
            tournamentId,
            "Initial Tournament Name",
            "SEASON-1",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(10),
            currentStatus
        );

        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Updated Tournament Name",
            SeasonIdentifier = "SEASON-1-MODIFIED",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(12)
        };

        var updatedRecord = new TournamentResponse(
            tournamentId,
            updateRequest.Name,
            updateRequest.SeasonIdentifier,
            updateRequest.StartDate,
            updateRequest.EndDate,
            currentStatus
        );

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);

        _mockRepository.Setup(r => r.UpdateTournamentAsync(
            tournamentId,
            updateRequest.Name,
            updateRequest.SeasonIdentifier,
            updateRequest.StartDate,
            updateRequest.EndDate
        )).ReturnsAsync(true);

        _mockRepository.SetupSequence(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing)
            .ReturnsAsync(updatedRecord);

        // Act
        var result = await _controller.UpdateTournament(tournamentId, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<TournamentResponse>(okResult.Value);
        Assert.Equal("Updated Tournament Name", response.Name);
        Assert.Equal("SEASON-1-MODIFIED", response.SeasonIdentifier);
    }

    [Fact]
    public async Task UpdateTournament_WhenCancelledStatus_RejectsWith400BadRequest()
    {
        // Arrange
        const int tournamentId = 11;
        var existing = new TournamentResponse(
            tournamentId,
            "Cancelled Tournament",
            "SEASON-C",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(10),
            TournamentStatus.Cancelled
        );

        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Should Not Update",
            SeasonIdentifier = "SEASON-C",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);

        // Act
        var result = await _controller.UpdateTournament(tournamentId, updateRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.UpdateTournamentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTournament_WhenCompletedStatus_RejectsWith400BadRequest()
    {
        // Arrange
        const int tournamentId = 12;
        var existing = new TournamentResponse(
            tournamentId,
            "Completed Tournament",
            "SEASON-COMP",
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(-1),
            TournamentStatus.Completed
        );

        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Should Not Update Completed",
            SeasonIdentifier = "SEASON-COMP",
            StartDate = DateTimeOffset.UtcNow.AddDays(-30),
            EndDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);

        // Act
        var result = await _controller.UpdateTournament(tournamentId, updateRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.UpdateTournamentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTournament_NonExistentTournament_Returns404NotFound()
    {
        // Arrange
        const int nonExistentId = 999;
        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Does Not Exist",
            SeasonIdentifier = "SEASON-NONE",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(5)
        };

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(nonExistentId))
            .ReturnsAsync((TournamentResponse?)null);

        // Act
        var result = await _controller.UpdateTournament(nonExistentId, updateRequest);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateTournament_EndDateBeforeStartDate_Returns400BadRequest()
    {
        // Arrange
        const int tournamentId = 15;
        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Invalid Range Update",
            SeasonIdentifier = "SEASON-INV",
            StartDate = DateTimeOffset.UtcNow.AddDays(10),
            EndDate = DateTimeOffset.UtcNow.AddDays(2) // Invalid
        };

        // Act
        var result = await _controller.UpdateTournament(tournamentId, updateRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.GetTournamentByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTournament_RepositoryFailure_Returns400BadRequest()
    {
        // Arrange
        const int tournamentId = 16;
        var existing = new TournamentResponse(
            tournamentId,
            "Active Tournament",
            "SEASON-ACT",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(10),
            TournamentStatus.Active
        );

        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Update Attempt",
            SeasonIdentifier = "SEASON-ACT",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);
        _mockRepository.Setup(r => r.UpdateTournamentAsync(tournamentId, updateRequest.Name, updateRequest.SeasonIdentifier, updateRequest.StartDate, updateRequest.EndDate))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateTournament(tournamentId, updateRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    #region Cancel Tournament Tests

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.Active)]
    public async Task CancelTournament_WhenScheduledOrActive_TransitionsToCancelledAndReturns200OK(string currentStatus)
    {
        // Arrange
        const int tournamentId = 20;
        var existing = new TournamentResponse(
            tournamentId,
            "Tournament to Cancel",
            "SEASON-CANCEL-ME",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(15),
            currentStatus
        );

        var cancelledResponse = new TournamentResponse(
            tournamentId,
            existing.Name,
            existing.SeasonIdentifier,
            existing.StartDate,
            existing.EndDate,
            TournamentStatus.Cancelled
        );

        _mockRepository.SetupSequence(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing)
            .ReturnsAsync(cancelledResponse);

        _mockRepository.Setup(r => r.CancelTournamentAsync(tournamentId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelTournament(tournamentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<TournamentResponse>(okResult.Value);
        Assert.Equal(TournamentStatus.Cancelled, response.Status);
        _mockRepository.Verify(r => r.CancelTournamentAsync(tournamentId), Times.Once);
    }

    [Fact]
    public async Task CancelTournament_WhenAlreadyCancelled_Returns400BadRequest()
    {
        // Arrange
        const int tournamentId = 21;
        var existing = new TournamentResponse(
            tournamentId,
            "Already Cancelled Tournament",
            "SEASON-CANC",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(5),
            TournamentStatus.Cancelled
        );

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);

        // Act
        var result = await _controller.CancelTournament(tournamentId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CancelTournamentAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelTournament_WhenCompleted_Returns400BadRequest()
    {
        // Arrange
        const int tournamentId = 22;
        var existing = new TournamentResponse(
            tournamentId,
            "Completed Tournament",
            "SEASON-COMP",
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(-5),
            TournamentStatus.Completed
        );

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);

        // Act
        var result = await _controller.CancelTournament(tournamentId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockRepository.Verify(r => r.CancelTournamentAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelTournament_NonExistentTournament_Returns404NotFound()
    {
        // Arrange
        const int nonExistentId = 999;
        _mockRepository.Setup(r => r.GetTournamentByIdAsync(nonExistentId))
            .ReturnsAsync((TournamentResponse?)null);

        // Act
        var result = await _controller.CancelTournament(nonExistentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        _mockRepository.Verify(r => r.CancelTournamentAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelTournament_AtomicConditionalUpdateFails_Returns400BadRequest()
    {
        // Arrange
        const int tournamentId = 23;
        var existing = new TournamentResponse(
            tournamentId,
            "Concurrent Cancel",
            "SEASON-CONC",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(10),
            TournamentStatus.Scheduled
        );

        _mockRepository.Setup(r => r.GetTournamentByIdAsync(tournamentId))
            .ReturnsAsync(existing);
        _mockRepository.Setup(r => r.CancelTournamentAsync(tournamentId))
            .ReturnsAsync(false); // Atomic conditional query modified 0 rows

        // Act
        var result = await _controller.CancelTournament(tournamentId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion
}
