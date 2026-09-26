namespace BattleEconomyService.Configuration;

/// <summary>
/// Configuration options for internal communication with StreamService (SCRUM-118).
/// </summary>
public class StreamServiceOptions
{
    public const string SectionName = "StreamService";

    /// <summary>
    /// Base URL of StreamService.
    /// Local dev default: http://localhost:5167
    /// Docker Compose default: http://stream:8080
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5167";

    /// <summary>
    /// Overall HTTP timeout ceiling in seconds for stream liveness checks (SCRUM-118: 2 seconds).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 2;

    /// <summary>
    /// Number of retries for transient HTTP failures (default: 1).
    /// </summary>
    public int RetryCount { get; set; } = 1;
}
