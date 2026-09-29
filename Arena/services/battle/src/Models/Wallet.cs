namespace BattleEconomyService.Models;

public class Wallet
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public int Coins { get; set; }
    public DateTime? LastTickAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
