using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChatService.Consumers;
using ChatService.Entities;
using ChatService.Repositories;
using Xunit;

namespace ChatService.Tests;

public class TeamCacheConsumerTests
{
    private readonly Mock<IChatTeamCacheRepository> _mockTeamCacheRepo;
    private readonly TeamCacheConsumer _consumer;

    public TeamCacheConsumerTests()
    {
        _mockTeamCacheRepo = new Mock<IChatTeamCacheRepository>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Kafka:BootstrapServers", "localhost:9092" },
            { "Kafka:GroupId", "test-group" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Build a minimal service provider with the mocked repository
        var services = new ServiceCollection();
        services.AddSingleton<IChatTeamCacheRepository>(_mockTeamCacheRepo.Object);
        var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = new Mock<ILogger<TeamCacheConsumer>>();

        _consumer = new TeamCacheConsumer(configuration, scopeFactory, logger.Object);
    }

    [Fact]
    public async Task ProcessMessage_ValidEvent_CallsUpsertAsync()
    {
        // Arrange
        var json = """{"teamId":1,"teamName":"Alpha","colorHex":"#FF0000","changeType":"Created","occurredAt":"2026-01-01T00:00:00Z"}""";

        // Act
        await _consumer.ProcessMessageAsync(json);

        // Assert
        _mockTeamCacheRepo.Verify(r => r.UpsertAsync(It.Is<ChatTeamCache>(c =>
            c.TeamId == 1 &&
            c.TeamName == "Alpha" &&
            c.TeamColor == "#FF0000"
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_UpdatedEvent_CallsUpsertAsync()
    {
        // Arrange
        var json = """{"teamId":5,"teamName":"Beta Updated","colorHex":"#00FF00","changeType":"Updated","occurredAt":"2026-06-15T12:00:00Z"}""";

        // Act
        await _consumer.ProcessMessageAsync(json);

        // Assert
        _mockTeamCacheRepo.Verify(r => r.UpsertAsync(It.Is<ChatTeamCache>(c =>
            c.TeamId == 5 &&
            c.TeamName == "Beta Updated" &&
            c.TeamColor == "#00FF00"
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_MalformedJson_DoesNotThrow()
    {
        // Arrange
        var json = "this is not valid json {{{";

        // Act & Assert — should not throw
        var exception = await Record.ExceptionAsync(() => _consumer.ProcessMessageAsync(json));
        Assert.Null(exception);

        // UpsertAsync should never be called
        _mockTeamCacheRepo.Verify(r => r.UpsertAsync(It.IsAny<ChatTeamCache>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessage_EmptyJson_DoesNotThrow()
    {
        // Arrange
        var json = "{}";

        // Act & Assert — should not throw (TeamId will be 0 which is <= 0, so it's skipped)
        var exception = await Record.ExceptionAsync(() => _consumer.ProcessMessageAsync(json));
        Assert.Null(exception);

        _mockTeamCacheRepo.Verify(r => r.UpsertAsync(It.IsAny<ChatTeamCache>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessage_NegativeTeamId_DoesNotUpsert()
    {
        // Arrange
        var json = """{"teamId":-1,"teamName":"Invalid","colorHex":"#000000","changeType":"Created"}""";

        // Act
        await _consumer.ProcessMessageAsync(json);

        // Assert
        _mockTeamCacheRepo.Verify(r => r.UpsertAsync(It.IsAny<ChatTeamCache>()), Times.Never);
    }
}
