/*
    unit tests for StreamsController verifying authorization, match validation,
    stream linking constraints, and public lookup endpoints
*/

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StreamService.Controllers;
using StreamService.DTOs;
using StreamService.Repositories;
using Xunit;

namespace StreamService.Tests.Controllers;

public class StreamsControllerTests
{
    private readonly Mock<IStreamRepository> _streamRepoMock;
    private readonly Mock<IMatchRepository> _matchRepoMock;
    private readonly StreamsController _controller;

    public StreamsControllerTests()
    {
        _streamRepoMock = new Mock<IStreamRepository>();
        _matchRepoMock = new Mock<IMatchRepository>();

        _controller = new StreamsController(
            _streamRepoMock.Object,
            _matchRepoMock.Object
        );
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

    /* ---------------- LinkStreamToMatch Tests ---------------- */

    [Fact]
    public async Task LinkStreamToMatch_WhenUserClaimMissingOrInvalid_Returns401Unauthorized()
    {
        // Arrange
        SetUserContext(null, "Organizer");
        var request = new LinkStreamRequest { ChannelName = "esl_csgo", Platform = "Twitch", EmbedParentDomain = "arena.gg" };

        // Act
        var result = await _controller.LinkStreamToMatch(1, request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task LinkStreamToMatch_WhenTargetMatchDoesNotExist_Returns404NotFound()
    {
        // Arrange
        SetUserContext("42", "Organizer");
        _matchRepoMock.Setup(m => m.GetMatchByIdAsync(999)).ReturnsAsync((MatchResponse?)null);
        var request = new LinkStreamRequest { ChannelName = "esl_csgo", Platform = "Twitch", EmbedParentDomain = "arena.gg" };

        // Act
        var result = await _controller.LinkStreamToMatch(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task LinkStreamToMatch_WhenStreamAlreadyLinkedToMatch_Returns409Conflict()
    {
        // Arrange
        const int matchId = 10;
        SetUserContext("42", "Streamer");

        var dummyMatch = new MatchResponse(matchId, 1, 100, 101, DateTimeOffset.UtcNow, "Scheduled", null, DateTime.UtcNow);
        _matchRepoMock.Setup(m => m.GetMatchByIdAsync(matchId)).ReturnsAsync(dummyMatch);
        _streamRepoMock.Setup(s => s.StreamExistsForMatchAsync(matchId)).ReturnsAsync(true);

        var request = new LinkStreamRequest { ChannelName = "esl_csgo", Platform = "Twitch", EmbedParentDomain = "arena.gg" };

        // Act
        var result = await _controller.LinkStreamToMatch(matchId, request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task LinkStreamToMatch_WhenValid_Returns201CreatedWithStreamResponse()
    {
        // Arrange
        const int matchId = 10;
        const int streamerId = 42;
        const int generatedStreamId = 500;
        SetUserContext(streamerId.ToString(), "Streamer");

        var dummyMatch = new MatchResponse(matchId, 1, 100, 101, DateTimeOffset.UtcNow, "Scheduled", null, DateTime.UtcNow);
        _matchRepoMock.Setup(m => m.GetMatchByIdAsync(matchId)).ReturnsAsync(dummyMatch);
        _streamRepoMock.Setup(s => s.StreamExistsForMatchAsync(matchId)).ReturnsAsync(false);

        var request = new LinkStreamRequest
        {
            ChannelName = "esl_csgo",
            Platform = "Twitch",
            StreamTitle = "Grand Finals",
            EmbedParentDomain = "arena.gg"
        };

        _streamRepoMock.Setup(s => s.LinkStreamToMatchAsync(matchId, streamerId, dummyMatch.TournamentId, request))
            .ReturnsAsync(generatedStreamId);

        var createdStream = new StreamResponse(
            generatedStreamId, streamerId, dummyMatch.TournamentId, matchId,
            "esl_csgo", "Twitch", "Grand Finals", "arena.gg", "Scheduled", 0, null, null, DateTime.UtcNow
        );

        _streamRepoMock.Setup(s => s.GetStreamByIdAsync(generatedStreamId)).ReturnsAsync(createdStream);

        // Act
        var result = await _controller.LinkStreamToMatch(matchId, request);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
        Assert.Equal(nameof(StreamsController.GetStreamById), createdAtResult.ActionName);

        var responseData = Assert.IsType<StreamResponse>(createdAtResult.Value);
        Assert.Equal(generatedStreamId, responseData.StreamId);
        Assert.Equal("esl_csgo", responseData.ChannelName);
    }

    [Theory]
    [InlineData("http://arena.gg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("arena.gg/path")]
    [InlineData("arena.gg:8080")]
    public async Task LinkStreamToMatch_WhenEmbedParentDomainIsInvalid_Returns400BadRequest(string invalidDomain)
    {
        // Arrange
        const int matchId = 10;
        SetUserContext("42", "Streamer");

        var request = new LinkStreamRequest { ChannelName = "esl_csgo", Platform = "Twitch", EmbedParentDomain = invalidDomain };

        // Act
        var result = await _controller.LinkStreamToMatch(matchId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    /* ---------------- Public Lookup Tests ---------------- */

    [Fact]
    public async Task GetStreamById_WhenStreamExists_Returns200OK()
    {
        // Arrange
        const int streamId = 100;
        var stream = new StreamResponse(streamId, 1, 1, 1, "esl_csgo", "Twitch", "Live Stream", "localhost", "Live", 1200, DateTime.UtcNow, null, DateTime.UtcNow);
        _streamRepoMock.Setup(s => s.GetStreamByIdAsync(streamId)).ReturnsAsync(stream);

        // Act
        var result = await _controller.GetStreamById(streamId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(stream, okResult.Value);
    }

    [Fact]
    public async Task GetStreamById_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        _streamRepoMock.Setup(s => s.GetStreamByIdAsync(999)).ReturnsAsync((StreamResponse?)null);

        // Act
        var result = await _controller.GetStreamById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetStreamByMatchId_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        _streamRepoMock.Setup(s => s.GetStreamByMatchIdAsync(999)).ReturnsAsync((StreamResponse?)null);

        // Act
        var result = await _controller.GetStreamByMatchId(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    /* ---------------- Update & Delete Ownership Tests ---------------- */

    [Theory]
    [InlineData("http://arena.gg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("arena.gg/path")]
    [InlineData("arena.gg:8080")]
    public async Task UpdateStream_WhenEmbedParentDomainIsInvalid_Returns400BadRequest(string invalidDomain)
    {
        // Arrange
        const int streamId = 10;
        SetUserContext("99", "Streamer");

        var request = new UpdateStreamRequest { ChannelName = "new_channel", Platform = "Twitch", EmbedParentDomain = invalidDomain };

        // Act
        var result = await _controller.UpdateStream(streamId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStream_WhenCallerIsNotOwnerNorOrganizer_Returns403Forbid()
    {
        // Arrange
        const int streamId = 10;
        SetUserContext("99", "Streamer");

        var existingStream = new StreamResponse(streamId, 42, 1, 1, "owner_channel", "Twitch", "Title", "localhost", "Scheduled", 0, null, null, DateTime.UtcNow);
        _streamRepoMock.Setup(s => s.GetStreamByIdAsync(streamId)).ReturnsAsync(existingStream);

        var request = new UpdateStreamRequest { ChannelName = "new_channel", Platform = "Twitch", EmbedParentDomain = "arena.gg" };

        // Act
        var result = await _controller.UpdateStream(streamId, request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteStream_WhenCallerIsOrganizer_Returns204NoContent()
    {
        // Arrange
        const int streamId = 10;
        SetUserContext("999", "Organizer");

        var existingStream = new StreamResponse(streamId, 42, 1, 1, "owner_channel", "Twitch", "Title", "localhost", "Scheduled", 0, null, null, DateTime.UtcNow);
        _streamRepoMock.Setup(s => s.GetStreamByIdAsync(streamId)).ReturnsAsync(existingStream);
        _streamRepoMock.Setup(s => s.DeleteStreamAsync(streamId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteStream(streamId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _streamRepoMock.Verify(s => s.DeleteStreamAsync(streamId), Times.Once);
    }
}