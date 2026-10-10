using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using AnalyticsService.Configuration;
using AnalyticsService.Repositories;
using Arena.Shared.EventContracts;

namespace AnalyticsService.Consumers;

/// <summary>
/// Background worker service that consumes <see cref="CoinEarnedEvent"/> messages from the
/// configured <c>coin.earned</c> Kafka topic and updates the stream engagement read model
/// in <c>arena_analytics_db</c>.
/// </summary>
public class EngagementEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafkaOptions;
    private readonly AnalyticsOptions _analyticsOptions;
    private readonly ILogger<EngagementEventConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EngagementEventConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<AnalyticsOptions> analyticsOptions,
        ILogger<EngagementEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _kafkaOptions = kafkaOptions.Value;
        _analyticsOptions = analyticsOptions.Value;
        _logger = logger;

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield execution to allow the ASP.NET Core web host to finish startup before consumer loop begins
        await Task.Yield();

        _logger.LogInformation(
            "EngagementEventConsumer starting — subscribing to topic {Topic} with GroupId {GroupId}",
            _kafkaOptions.CoinEarnedTopic, _kafkaOptions.GroupId);

        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(_kafkaOptions.CoinEarnedTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value == null)
                    {
                        continue;
                    }

                    await ProcessMessageAsync(result.Message.Value);

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on topic {Topic}: {Reason}",
                        _kafkaOptions.CoinEarnedTopic, ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("EngagementEventConsumer received cancellation token, shutting down gracefully");
        }
        finally
        {
            consumer.Close();
        }
    }

    /// <summary>
    /// Deserializes the <see cref="CoinEarnedEvent"/> JSON payload and records the award in the read model.
    /// Marked public and virtual to enable isolated unit testing without a live Kafka broker.
    /// </summary>
    /// <param name="messageValue">Raw JSON string from the Kafka message.</param>
    /// <returns>True if successfully processed; false if skipped or invalid.</returns>
    public virtual async Task<bool> ProcessMessageAsync(string messageValue)
    {
        CoinEarnedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<CoinEarnedEvent>(messageValue, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize CoinEarnedEvent JSON — skipping payload: {Message}", messageValue);
            return false;
        }

        if (evt == null)
        {
            _logger.LogWarning("Deserialized CoinEarnedEvent was null — skipping message");
            return false;
        }

        // MatchId in CoinEarnedEvent represents the stream_id/match being watched.
        // If MatchId is null or non-positive, this watch tick occurred without stream context.
        if (!evt.MatchId.HasValue || evt.MatchId.Value <= 0)
        {
            _logger.LogInformation(
                "CoinEarnedEvent for user {UserId} has no valid stream/match context (MatchId: {MatchId}) — skipping engagement aggregation",
                evt.UserId, evt.MatchId);
            return false;
        }

        var streamId = evt.MatchId.Value;
        var watchDuration = _analyticsOptions.DefaultWatchTickDurationSeconds;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();

        var updated = await repository.RecordCoinEarnedAsync(
            streamId: streamId,
            userId: evt.UserId,
            amount: evt.Amount,
            timestampUtc: evt.TimestampUtc,
            watchDurationSeconds: watchDuration);

        if (updated)
        {
            _logger.LogInformation(
                "Recorded engagement for stream {StreamId} from user {UserId}: +{Coins} coins, +{Seconds}s watch time",
                streamId, evt.UserId, evt.Amount, watchDuration);
        }
        else
        {
            _logger.LogDebug(
                "Skipped duplicate CoinEarnedEvent for stream {StreamId}, user {UserId} at {Timestamp}",
                streamId, evt.UserId, evt.TimestampUtc);
        }

        return updated;
    }
}
