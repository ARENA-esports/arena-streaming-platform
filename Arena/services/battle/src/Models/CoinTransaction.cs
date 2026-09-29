namespace BattleEconomyService.Models;

public class CoinTransaction
{
    public long TransactionId { get; set; }
    public int UserId { get; set; }
    public int WalletId { get; set; }
    public int Amount { get; set; }
    public string TransactionType { get; set; } = "WATCH_TICK";
    public int? StreamId { get; set; }
    public DateTime CreatedAt { get; set; }
}
