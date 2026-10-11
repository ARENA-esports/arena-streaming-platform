namespace BattleEconomyService.DTOs;

/// <summary>
/// Encapsulates the atomic result of wallet deduction, attack logging, and coin transaction recording (Finding 1).
/// </summary>
public class AtomicAttackResult
{
    public bool Success { get; set; }
    public long AttackId { get; set; }
    public int RemainingBalance { get; set; }
    public int WalletId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool InsufficientFunds { get; set; }

    public static AtomicAttackResult Succeeded(long attackId, int remainingBalance, int walletId) =>
        new()
        {
            Success = true,
            AttackId = attackId,
            RemainingBalance = remainingBalance,
            WalletId = walletId
        };

    public static AtomicAttackResult InsufficientCoins(int currentBalance) =>
        new()
        {
            Success = false,
            InsufficientFunds = true,
            RemainingBalance = currentBalance,
            ErrorMessage = "Insufficient coins to purchase this weapon."
        };

    public static AtomicAttackResult Failed(string errorMessage, int currentBalance = 0) =>
        new()
        {
            Success = false,
            RemainingBalance = currentBalance,
            ErrorMessage = errorMessage
        };
}
