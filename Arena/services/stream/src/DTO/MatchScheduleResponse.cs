namespace StreamService.DTOs;

public record TeamSummary(int TeamId, string name, string colorHex, string? LogoUrl);

public record MatchScheduleResponse(
    int MatchId,
    int TournamentId,
    DateTimeOffset ScheduledTime,
    string Status,
    TeamSummary TeamA,
    TeamSummary TeamB
);