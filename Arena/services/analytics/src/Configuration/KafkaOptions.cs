namespace AnalyticsService.Configuration;

/// <summary>
/// Configuration options for Kafka consumption within the Analytics service.
/// Bound from the <c>"Kafka"</c> section of application configuration.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Comma-separated list of host:port pairs for Kafka bootstrap servers.
    /// Default: <c>localhost:9092</c>.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Unique Kafka consumer group identifier for the Analytics service.
    /// Default: <c>analytics-service-engagement</c>.
    /// </summary>
    public string GroupId { get; set; } = "analytics-service-engagement";

    /// <summary>
    /// Topic from which coin-earned / watch-tick events are consumed.
    /// Default: <c>coin.earned</c>.
    /// </summary>
    public string CoinEarnedTopic { get; set; } = "coin.earned";
}
