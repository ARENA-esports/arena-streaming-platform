namespace BattleEconomyService.Models;

/// <summary>
/// Standard stream lifecycle states matching StreamService.Models.StreamStatus.
/// Used by SCRUM-118 to cross-check broadcast liveness before awarding coins.
/// </summary>
public static class StreamLiveStatus
{
    public const string Scheduled = "Scheduled";
    public const string Live = "Live";
    public const string Ended = "Ended";
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// Returns true if the status represents an active live broadcast ("Live").
    /// Comparison is case-insensitive.
    /// </summary>
    public static bool IsLive(string? status) =>
        string.Equals(status, Live, StringComparison.OrdinalIgnoreCase);
}
