namespace TournamentService.Models;

public static class TournamentStatus
{
    public const string Scheduled = "Scheduled";
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] AllowedStatuses = { Scheduled, Active, Completed, Cancelled };

    public static bool IsValid(string status) =>
        AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
}
