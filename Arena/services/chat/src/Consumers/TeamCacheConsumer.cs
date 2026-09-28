using System.Text.Json;
using Confluent.Kafka;
using ChatService.Entities;
using ChatService.Repositories;
using EventContracts;

namespace ChatService.Consumers;

/// <summary>
/// Background service that consumes TeamChangedEvent messages from the "arena.teams.changed" Kafka topic
/// and upserts team metadata into the local chat_team_cache table.
/// </summary>
public class TeamCacheConsumer : BackgroundService
{
    private const string TopicName = "arena.teams.changed";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TeamCacheConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TeamCacheConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<TeamCacheConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:GroupId"] ?? "chat-service-team-cache",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to let the host finish starting up before we start consuming
        await Task.Yield();

        _logger.LogInformation("TeamCacheConsumer starting — subscribing to {Topic}", TopicName);

        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(TopicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value == null)
                        continue;

                    await ProcessMessageAsync(result.Message.Value);

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on topic {Topic}", TopicName);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TeamCacheConsumer shutting down gracefully");
        }
        finally
        {
            consumer.Close();
        }
    }

    /// <summary>
    /// Deserializes the event payload and upserts into the local team cache.
    /// Visible for unit testing.
    /// </summary>
    public async Task ProcessMessageAsync(string messageValue)
    {
        TeamChangedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<TeamChangedEvent>(messageValue, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize TeamChangedEvent — skipping message: {Message}", messageValue);
            return;
        }

        if (evt == null || evt.TeamId <= 0)
        {
            _logger.LogWarning("Received invalid TeamChangedEvent (null or invalid TeamId) — skipping");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var teamCacheRepo = scope.ServiceProvider.GetRequiredService<IChatTeamCacheRepository>();

        var cacheEntry = new ChatTeamCache
        {
            TeamId = evt.TeamId,
            TeamName = evt.TeamName,
            TeamColor = evt.ColorHex
        };

        await teamCacheRepo.UpsertAsync(cacheEntry);

        _logger.LogInformation(
            "Consumed TeamChangedEvent: TeamId={TeamId}, TeamName={TeamName}, ColorHex={ColorHex}, ChangeType={ChangeType}",
            evt.TeamId, evt.TeamName, evt.ColorHex, evt.ChangeType);
    }
}
