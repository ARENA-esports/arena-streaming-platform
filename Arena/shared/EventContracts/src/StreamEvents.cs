namespace Arena.Shared.EventContracts;

/// <summary>
/// Event published when a stream transitions to Live status.
/// </summary>
public record StreamStartedEvent (
    int streamId,
    int MatchId,
    int streamerUserId,
    string ChannelName,
    string StreamTitle,
    string Platform,
    DateTime Timestamp
);


/// <summary>
/// Event published when a stream transitions to Ended status.
/// </summary>
public record StreamEndedEvent (
    int StreamId,
    int MatchId,
    string ChannelName,
    DateTime Timestamp
);


/// <summary>
/// Event published when team details or branding are updated.
/// </summary>
public record TeamUpdatedEvent (
    int TeamId,
    string TeamName,
    string ColorHex,
    string? LogoUrl,
    DateTime Timestamp
);

/// <summary>
/// Event published when a roster player is added to a team.
/// </summary>
public record TeamRosterUpdatedEvent(
    int TeamId,
    int PlayerId,
    DateTime Timestamp
);

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