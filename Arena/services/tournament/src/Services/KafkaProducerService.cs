using System.Text.Json;
using Confluent.Kafka;
using EventContracts;

namespace TournamentService.Services;

/// <summary>
/// Kafka producer that publishes TeamChangedEvent messages to the "arena.teams.changed" topic.
/// Registered as a singleton to reuse the underlying Confluent IProducer connection.
/// </summary>
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private const string TopicName = "arena.teams.changed";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.Leader,
            MessageTimeoutMs = 5000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishTeamChangedAsync(TeamChangedEvent evt)
    {
        var key = evt.TeamId.ToString();
        var value = JsonSerializer.Serialize(evt, JsonOptions);

        try
        {
            var result = await _producer.ProduceAsync(TopicName, new Message<string, string>
            {
                Key = key,
                Value = value
            });

            _logger.LogInformation(
                "Published TeamChangedEvent to {Topic} [Partition={Partition}, Offset={Offset}]: TeamId={TeamId}, ChangeType={ChangeType}",
                TopicName, result.Partition.Value, result.Offset.Value, evt.TeamId, evt.ChangeType);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish TeamChangedEvent for TeamId={TeamId} to {Topic}",
                evt.TeamId, TopicName);
            // Fire-and-forget: log but don't rethrow — team creation/update should not fail
            // just because Kafka is temporarily unavailable
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
