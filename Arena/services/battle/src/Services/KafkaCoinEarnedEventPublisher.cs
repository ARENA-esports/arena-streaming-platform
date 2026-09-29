using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BattleEconomyService.Configuration;

// Alias to disambiguate from the internal BattleEconomyService.Services.CoinEarnedEvent
using KafkaContract = Arena.Shared.EventContracts.CoinEarnedEvent;

namespace BattleEconomyService.Services;

/// <summary>
/// Kafka producer implementation of <see cref="ICoinEarnedEventPublisher"/>.
/// Serialises the internal watch-tick award event to the shared
/// <see cref="KafkaContract"/> Kafka payload shape and produces it to the
/// configured <c>arena.coin-earned</c> topic (SCRUM-117).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transactional ordering:</strong> this publisher is invoked by
/// <see cref="WatchTickService"/> only <em>after</em> the coin award has been
/// atomically committed to the database. A Kafka produce failure therefore
/// cannot roll back the award and is logged-and-swallowed rather than
/// propagated, to avoid returning a false-negative HTTP response to the viewer.
/// </para>
/// <para>
/// <strong>MatchId gap:</strong> the current watch-tick data model exposes
/// <c>StreamId</c> (the live stream being watched) but has no separate
/// <c>MatchId</c>. <c>StreamId</c> is mapped to <c>MatchId</c> in the Kafka
/// contract because the stream is the match context in the current architecture.
/// A dedicated <c>MatchId</c> field will require a future data-model change.
/// </para>
/// <para>
/// <strong>Producer lifetime:</strong> the producer is created per DI scope
/// (matching the current <c>Scoped</c> registration) and flushed on disposal.
/// Commit 4 promotes the registration to <c>Singleton</c> so a single
/// long-lived producer is shared across all requests.
/// </para>
/// </remarks>
public sealed class KafkaCoinEarnedEventPublisher : ICoinEarnedEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaCoinEarnedEventPublisher> _logger;

    // Shared, immutable serialiser options — camelCase to match JSON conventions
    // used throughout the Arena platform (System.Text.Json default for ASP.NET).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaCoinEarnedEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaCoinEarnedEventPublisher> logger)
    {
        var kafkaOptions = options.Value;
        _topic = kafkaOptions.Topic;
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    /// <inheritdoc />
    public async Task PublishCoinEarnedAsync(CoinEarnedEvent evt)
    {
        // Map internal service event → shared Kafka contract.
        // StreamId → MatchId: no dedicated MatchId exists in the current model;
        // StreamId represents the stream/match context and is the correct mapping.
        var contract = new KafkaContract(
            UserId: evt.UserId,
            MatchId: evt.StreamId,
            Amount: evt.Amount,
            TimestampUtc: evt.AwardedAt
        );

        var payload = JsonSerializer.Serialize(contract, JsonOptions);

        // Partition key = UserId string — guarantees per-user event ordering on
        // the topic without requiring a dedicated partitioning strategy.
        var message = new Message<string, string>
        {
            Key = evt.UserId.ToString(),
            Value = payload
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(_topic, message);

            _logger.LogInformation(
                "CoinEarned event published for user {UserId} to {Topic} " +
                "[partition={Partition} offset={Offset}]",
                evt.UserId,
                _topic,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            // The coin award is already committed to the database at this point
            // (see WatchTickService step 5 comment). Propagating this exception
            // would return HTTP 500 to the viewer for a successful award, which
            // is incorrect. Log at Error for observability and continue.
            _logger.LogError(
                ex,
                "Failed to publish CoinEarned event for user {UserId} to topic {Topic}: {Reason}",
                evt.UserId,
                _topic,
                ex.Error.Reason);
        }
    }

    /// <summary>
    /// Flushes any pending produce requests and releases the underlying
    /// librdkafka producer handle.
    /// </summary>
    public void Dispose()
    {
        // Flush with a timeout to drain in-flight messages before the
        // producer handle is released. Runs synchronously on disposal
        // because IDisposable does not support async.
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
