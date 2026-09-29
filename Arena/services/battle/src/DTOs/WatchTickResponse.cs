namespace BattleEconomyService.DTOs;

public class WatchTickResponse
{
    public bool Success { get; set; }
    public int CoinsAwarded { get; set; }
    public int CurrentBalance { get; set; }
    public DateTime? LastTickAt { get; set; }
    public int? RemainingSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
