using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BattleEconomyService.Configuration;
using BattleEconomyService.Services;
using Arena.Shared.EventContracts;
using SharedCoinEarnedEvent = Arena.Shared.EventContracts.CoinEarnedEvent;
using InternalCoinEarnedEvent = BattleEconomyService.Services.CoinEarnedEvent;

namespace BattleEconomyService.Tests;

public class KafkaCoinEarnedEventPublisherTests
{
    private readonly Mock<IProducer<string, string>> _producerMock;
    private readonly Mock<ILogger<KafkaCoinEarnedEventPublisher>> _loggerMock;
    private const string DefaultTopic = "arena.coin-earned";

    public KafkaCoinEarnedEventPublisherTests()
    {
        _producerMock = new Mock<IProducer<string, string>>();
        _loggerMock = new Mock<ILogger<KafkaCoinEarnedEventPublisher>>();

        // Default successful delivery result
        _producerMock
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string topic, Message<string, string> msg, CancellationToken ct) =>
                new DeliveryResult<string, string>
                {
                    Topic = topic,
                    Partition = new Partition(0),
                    Offset = new Offset(100),
                    Status = PersistenceStatus.Persisted,
                    Message = msg
                });
    }

    /// <summary>
    /// Helper to instantiate <see cref="KafkaCoinEarnedEventPublisher"/> with a test double
    /// for <see cref="IProducer{string, string}"/> without requiring a live Kafka broker.
    /// </summary>
    private KafkaCoinEarnedEventPublisher CreatePublisher(
        string topic = DefaultTopic,
        IProducer<string, string>? producer = null,
        ILogger<KafkaCoinEarnedEventPublisher>? logger = null)
    {
        var publisher = (KafkaCoinEarnedEventPublisher)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(KafkaCoinEarnedEventPublisher));

        var type = typeof(KafkaCoinEarnedEventPublisher);
        type.GetField("_producer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(publisher, producer ?? _producerMock.Object);
        type.GetField("_topic", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(publisher, topic);
        type.GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(publisher, logger ?? _loggerMock.Object);

        return publisher;
    }

    // =========================================================================
    // 1. Successful publishing path & contract mapping
    // =========================================================================

    [Fact]
    public async Task PublishCoinEarnedAsync_WhenInvoked_ProducesToConfiguredTopic()
    {
        // Arrange
        const string expectedTopic = "arena.custom-coin-topic";
        var publisher = CreatePublisher(topic: expectedTopic);
        var evt = new InternalCoinEarnedEvent(
            UserId: 42,
            WalletId: 10,
            Amount: 15,
            NewBalance: 115,
            StreamId: 101,
            AwardedAt: DateTime.UtcNow
        );

        // Act
        await publisher.PublishCoinEarnedAsync(evt);

        // Assert
        _producerMock.Verify(
            p => p.ProduceAsync(
                expectedTopic,
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishCoinEarnedAsync_UsesUserIdAsStringPartitionKey()
    {
        // Arrange
        var publisher = CreatePublisher();
        const int userId = 9876;
        var evt = new InternalCoinEarnedEvent(
            UserId: userId,
            WalletId: 1,
            Amount: 10,
            NewBalance: 50,
            StreamId: 55,
            AwardedAt: DateTime.UtcNow
        );

        Message<string, string>? capturedMessage = null;
        _producerMock
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((_, msg, _) => capturedMessage = msg)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await publisher.PublishCoinEarnedAsync(evt);

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal("9876", capturedMessage.Key);
    }

    [Fact]
    public async Task PublishCoinEarnedAsync_SerializesSharedContractWithCamelCasePayload()
    {
        // Arrange
        var publisher = CreatePublisher();
        var awardedAt = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
        var evt = new InternalCoinEarnedEvent(
            UserId: 1234,
            WalletId: 88,
            Amount: 25,
            NewBalance: 200,
            StreamId: 777,
            AwardedAt: awardedAt
        );

        Message<string, string>? capturedMessage = null;
        _producerMock
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((_, msg, _) => capturedMessage = msg)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await publisher.PublishCoinEarnedAsync(evt);

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.NotNull(capturedMessage.Value);

        using var doc = JsonDocument.Parse(capturedMessage.Value);
        var root = doc.RootElement;

        // Verify JSON camelCase property naming
        Assert.True(root.TryGetProperty("userId", out var userIdProp));
        Assert.Equal(1234, userIdProp.GetInt32());

        Assert.True(root.TryGetProperty("matchId", out var matchIdProp));
        Assert.Equal(777, matchIdProp.GetInt32());

        Assert.True(root.TryGetProperty("amount", out var amountProp));
        Assert.Equal(25, amountProp.GetInt32());

        Assert.True(root.TryGetProperty("timestampUtc", out var timestampProp));
        Assert.Equal(awardedAt, timestampProp.GetDateTime().ToUniversalTime());

        // Verify shared contract deserialization
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var sharedEvent = JsonSerializer.Deserialize<SharedCoinEarnedEvent>(capturedMessage.Value, jsonOptions);

        Assert.NotNull(sharedEvent);
        Assert.Equal(evt.UserId, sharedEvent.UserId);
        Assert.Equal(evt.StreamId, sharedEvent.MatchId);
        Assert.Equal(evt.Amount, sharedEvent.Amount);
        Assert.Equal(evt.AwardedAt, sharedEvent.TimestampUtc);
    }

    // =========================================================================
    // 2. StreamId -> MatchId mapping verification
    // =========================================================================

    [Fact]
    public async Task PublishCoinEarnedAsync_WhenStreamIdIsProvided_MapsToMatchId()
    {
        // Arrange
        var publisher = CreatePublisher();
        const int expectedStreamId = 456;
        var evt = new InternalCoinEarnedEvent(
            UserId: 1,
            WalletId: 1,
            Amount: 10,
            NewBalance: 10,
            StreamId: expectedStreamId,
            AwardedAt: DateTime.UtcNow
        );

        string? payload = null;
        _producerMock
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((_, msg, _) => payload = msg.Value)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await publisher.PublishCoinEarnedAsync(evt);

        // Assert
        Assert.NotNull(payload);
        using var doc = JsonDocument.Parse(payload);
        Assert.Equal(expectedStreamId, doc.RootElement.GetProperty("matchId").GetInt32());
    }

    [Fact]
    public async Task PublishCoinEarnedAsync_WhenStreamIdIsNull_MapsToNullMatchId()
    {
        // Arrange
        var publisher = CreatePublisher();
        var evt = new InternalCoinEarnedEvent(
            UserId: 1,
            WalletId: 1,
            Amount: 10,
            NewBalance: 10,
            StreamId: null,
            AwardedAt: DateTime.UtcNow
        );

        string? payload = null;
        _producerMock
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((_, msg, _) => payload = msg.Value)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                Partition = new Partition(0),
                Offset = new Offset(1)
            });

        // Act
        await publisher.PublishCoinEarnedAsync(evt);

        // Assert
        Assert.NotNull(payload);
        using var doc = JsonDocument.Parse(payload);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("matchId").ValueKind);
    }

    // =========================================================================
    // 3. Exception handling: produce failure resilience
    // =========================================================================

    [Fact]
    public async Task PublishCoinEarnedAsync_WhenProduceThrowsProduceException_SwallowsAndLogsError()
    {
        // Arrange
        var publisher = CreatePublisher();
        var evt = new InternalCoinEarnedEvent(
            UserId: 100,
            WalletId: 1,
            Amount: 10,
            NewBalance: 100,
            StreamId: 200,
            AwardedAt: DateTime.UtcNow
        );

        var produceError = new Error(ErrorCode.Local_Transport, "Simulated Kafka transport disconnection");
        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = DefaultTopic,
            Partition = new Partition(0),
            Offset = new Offset(-1)
        };
        var produceException = new ProduceException<string, string>(produceError, deliveryResult);

        _producerMock
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(produceException);

        // Act & Assert
        // Must complete normally without throwing, because the coin award was already committed to DB
        var exception = await Record.ExceptionAsync(() => publisher.PublishCoinEarnedAsync(evt));
        Assert.Null(exception);

        // Verify error logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish CoinEarned event")),
                produceException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // 4. Disposal behavior
    // =========================================================================

    [Fact]
    public void Dispose_WhenCalled_FlushesAndDisposesProducer()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        publisher.Dispose();

        // Assert
        _producerMock.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
        _producerMock.Verify(p => p.Dispose(), Times.Once);
    }
}
