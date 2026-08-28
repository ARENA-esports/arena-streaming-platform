namespace StreamService.DTOs;   // use c# file scope

public record StreamResponse(
    int StreamId,
    int StreamerId,     // user identifier extracted from token
    int? TournamentId,
    int? MatchId,
    string ChannelName,   // not nullable string holding for stream configuration
    string Platform,
    string StreamTitle,
    string? EmbedParentDomain,      // represent authorized host domain required by twitch TOS
    string Status,       // stream life cycle state("Scheduled", "Live", "Ended", "Cancelled")
    int ViewerCount,      // current viewer metric
    DateTime? StartedAt,    // when live event triggered
    DateTime? EndedAt,
    DateTime CreateAt       // UTC timestamp when record inserted
)


{
    // Generates the compliant iframe embed URL for frontend consumption
    public string EmbedUrl => Platform.Equals("Twitch", StringComparison.OrdinalIgnoreCase)
        ? $"https://player.twitch.tv/?channel={ChannelName}&parent={EmbedParentDomain ?? "localhost"}&autoplay=false"
        : $"https://www.youtube.com/embed/live_stream?channel={ChannelName}";
}