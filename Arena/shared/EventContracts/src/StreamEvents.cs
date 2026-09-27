namespace Arena.shared.EventContracts;

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