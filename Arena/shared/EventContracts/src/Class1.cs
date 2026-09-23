namespace EventContracts;

/// <summary>
/// Published to the "arena.teams.changed" Kafka topic whenever a team is created or updated
/// in the Tournament Service. Consumed by the Chat Service to keep its local team cache in sync.
/// </summary>
public class TeamChangedEvent
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "Created"; // "Created" or "Updated"
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
