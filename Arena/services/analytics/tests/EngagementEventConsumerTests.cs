using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AnalyticsService.Configuration;
using AnalyticsService.Consumers;
using AnalyticsService.Repositories;
using Arena.Shared.EventContracts;

namespace AnalyticsService.Tests;

public class EngagementEventConsumerTests
{
    private readonly Mock<IAnalyticsRepository> _repositoryMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<EngagementEventConsumer>> _loggerMock;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly IOptions<AnalyticsOptions> _analyticsOptions;

    public EngagementEventConsumerTests()
    {
        _repositoryMock = new Mock<IAnalyticsRepository>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<EngagementEventConsumer>>();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAnalyticsRepository)))
            .Returns(_repositoryMock.Object);

        _scopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(sf => sf.CreateScope())
            .Returns(_scopeMock.Object);

        _kafkaOptions = Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            CoinEarnedTopic = "coin.earned"
        });

        _analyticsOptions = Options.Create(new AnalyticsOptions
        {
            DefaultWatchTickDurationSeconds = 60
        });
    }

    private EngagementEventConsumer CreateConsumer(int? watchTickDuration = null)
    {
        var analyticsOptions = watchTickDuration.HasValue
            ? Options.Create(new AnalyticsOptions { DefaultWatchTickDurationSeconds = watchTickDuration.Value })
            : _analyticsOptions;

        return new EngagementEventConsumer(
            _scopeFactoryMock.Object,
            _kafkaOptions,
            analyticsOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_ValidCoinEarnedEvent_InvokesRepositoryWithCorrectStreamAndWatchTime()
    {
        // Arrange
        var consumer = CreateConsumer(watchTickDuration: 60);
        var timestamp = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        var contract = new CoinEarnedEvent(
            UserId: 42,
            MatchId: 101, // StreamId
            Amount: 10,
            TimestampUtc: timestamp);

        var json = JsonSerializer.Serialize(contract, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _repositoryMock
            .Setup(r => r.RecordCoinEarnedAsync(101, 42, 10, timestamp, 60))
            .ReturnsAsync(true);

        // Act
        var result = await consumer.ProcessMessageAsync(json);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(
            r => r.RecordCoinEarnedAsync(101, 42, 10, timestamp, 60),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_UsesConfiguredWatchTickDurationSeconds()
    {
        // Arrange
        var consumer = CreateConsumer(watchTickDuration: 55); // Custom 55s matching server economy
        var timestamp = DateTime.UtcNow;
        var contract = new CoinEarnedEvent(
            UserId: 7,
            MatchId: 202,
            Amount: 15,
            TimestampUtc: timestamp);

        var json = JsonSerializer.Serialize(contract);

        _repositoryMock
            .Setup(r => r.RecordCoinEarnedAsync(202, 7, 15, timestamp, 55))
            .ReturnsAsync(true);

        // Act
        var result = await consumer.ProcessMessageAsync(json);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(
            r => r.RecordCoinEarnedAsync(202, 7, 15, timestamp, 55),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_NullMatchId_SkipsProcessingWithoutCallingRepository()
    {
        // Arrange
        var consumer = CreateConsumer();
        var contract = new CoinEarnedEvent(
            UserId: 42,
            MatchId: null, // No stream context
            Amount: 10,
            TimestampUtc: DateTime.UtcNow);

        var json = JsonSerializer.Serialize(contract);

        // Act
        var result = await consumer.ProcessMessageAsync(json);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(
            r => r.RecordCoinEarnedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task ProcessMessageAsync_NonPositiveMatchId_SkipsProcessingWithoutCallingRepository(int invalidMatchId)
    {
        // Arrange
        var consumer = CreateConsumer();
        var contract = new CoinEarnedEvent(
            UserId: 42,
            MatchId: invalidMatchId,
            Amount: 10,
            TimestampUtc: DateTime.UtcNow);

        var json = JsonSerializer.Serialize(contract);

        // Act
        var result = await consumer.ProcessMessageAsync(json);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(
            r => r.RecordCoinEarnedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_MalformedJson_HandlesExceptionAndReturnsFalse()
    {
        // Arrange
        var consumer = CreateConsumer();
        var malformedJson = "{ invalid-json-payload-without-quotes ";

        // Act
        var result = await consumer.ProcessMessageAsync(malformedJson);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(
            r => r.RecordCoinEarnedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_DuplicateEvent_ReturnsFalseWhenRepositoryDetectsDuplicate()
    {
        // Arrange
        var consumer = CreateConsumer();
        var timestamp = DateTime.UtcNow;
        var contract = new CoinEarnedEvent(
            UserId: 99,
            MatchId: 303,
            Amount: 10,
            TimestampUtc: timestamp);

        var json = JsonSerializer.Serialize(contract);

        // Repository returns false to indicate duplicate (idempotency check failed)
        _repositoryMock
            .Setup(r => r.RecordCoinEarnedAsync(303, 99, 10, timestamp, 60))
            .ReturnsAsync(false);

        // Act
        var result = await consumer.ProcessMessageAsync(json);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(
            r => r.RecordCoinEarnedAsync(303, 99, 10, timestamp, 60),
            Times.Once);
    }
}
