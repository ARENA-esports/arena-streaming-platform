namespace BattleEconomyService.DTOs;

public class WatchTickRequest
{
    /// <summary>
    /// Optional context: The stream being watched by the viewer.
    /// </summary>
    public int? StreamId { get; set; }
}
