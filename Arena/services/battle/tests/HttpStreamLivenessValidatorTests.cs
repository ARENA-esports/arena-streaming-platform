using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Services;

namespace BattleEconomyService.Tests;

public class HttpStreamLivenessValidatorTests
{
    private readonly Mock<IStreamServiceClient> _clientMock = new();
    private readonly Mock<ILogger<HttpStreamLivenessValidator>> _loggerMock = new();

    private HttpStreamLivenessValidator CreateValidator() =>
        new(_clientMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientReturnsLive_ReturnsTrue()
    {
        // Arrange
        const int streamId = 101;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StreamLivenessResult.Live(streamId));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientReturnsEnded_ReturnsFalse()
    {
        // Arrange
        const int streamId = 101;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StreamLivenessResult.NotLive(streamId, StreamLiveStatus.Ended));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientReturnsCancelled_ReturnsFalse()
    {
        // Arrange
        const int streamId = 101;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StreamLivenessResult.NotLive(streamId, StreamLiveStatus.Cancelled));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientReturnsScheduled_ReturnsFalse()
    {
        // Arrange
        const int streamId = 101;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StreamLivenessResult.NotLive(streamId, StreamLiveStatus.Scheduled));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientReturnsNotFound_ReturnsFalse()
    {
        // Arrange
        const int streamId = 404;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StreamLivenessResult.NotFound(streamId));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientReturnsTimeoutOrFailure_ReturnsFalse()
    {
        // Arrange
        const int streamId = 500;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StreamLivenessResult.Failed(streamId, "StreamService request timed out."));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenStreamIdIsNull_PreservesContractAndReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(null);

        // Assert
        Assert.True(result);
        _clientMock.Verify(c => c.GetStreamLivenessAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-42)]
    public async Task ValidateStreamLiveAsync_WhenStreamIdIsNonPositive_FailsSafeAndReturnsFalse(int invalidStreamId)
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(invalidStreamId);

        // Assert
        Assert.False(result);
        _clientMock.Verify(c => c.GetStreamLivenessAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_PassesCancellationTokenThroughToClient()
    {
        // Arrange
        const int streamId = 101;
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, token))
            .ReturnsAsync(StreamLivenessResult.Live(streamId));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId, token);

        // Assert
        Assert.True(result);
        _clientMock.Verify(c => c.GetStreamLivenessAsync(streamId, token), Times.Once);
    }

    [Fact]
    public async Task ValidateStreamLiveAsync_WhenClientThrowsUnexpectedException_FailsSafeAndReturnsFalse()
    {
        // Arrange
        const int streamId = 101;
        _clientMock.Setup(c => c.GetStreamLivenessAsync(streamId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected transport failure"));

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateStreamLiveAsync(streamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DependencyInjection_ResolvesHttpStreamLivenessValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => _clientMock.Object);
        services.AddScoped<IStreamLivenessValidator, HttpStreamLivenessValidator>();

        var provider = services.BuildServiceProvider();

        // Act
        var resolved = provider.GetRequiredService<IStreamLivenessValidator>();

        // Assert
        Assert.NotNull(resolved);
        Assert.IsType<HttpStreamLivenessValidator>(resolved);
    }
}
