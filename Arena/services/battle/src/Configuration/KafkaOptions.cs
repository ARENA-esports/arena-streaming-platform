namespace BattleEconomyService.Configuration;

/// <summary>
/// Configuration options for the Kafka producer used to publish
/// the <c>CoinEarned</c> event after a successful watch-tick award (SCRUM-117).
/// Bound from the <c>"Kafka"</c> section of application configuration.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Comma-separated list of host:port pairs for Kafka bootstrap servers.
    /// Local dev default: <c>localhost:9092</c>.
    /// Override via environment variable <c>Kafka__BootstrapServers</c> in Docker Compose.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Kafka topic name to which <c>CoinEarned</c> events are produced.
    /// </summary>
    public string Topic { get; set; } = "arena.coin-earned";
}
