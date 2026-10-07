using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;

namespace BattleEconomyService.Services;

public class WeaponShopService : IWeaponShopService
{
    private readonly IWeaponRepository _weaponRepository;
    private readonly IAttackRepository _attackRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly ILogger<WeaponShopService> _logger;

    public WeaponShopService(
        IWeaponRepository weaponRepository,
        IAttackRepository attackRepository,
        IWalletRepository walletRepository,
        ICoinTransactionRepository coinTransactionRepository,
        ILogger<WeaponShopService> logger)
    {
        _weaponRepository = weaponRepository;
        _attackRepository = attackRepository;
        _walletRepository = walletRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _logger = logger;
    }

    public async Task<List<WeaponDto>> GetWeaponsAsync()
    {
        var weapons = await _weaponRepository.GetActiveWeaponsAsync();
        return weapons.Select(w => new WeaponDto
        {
            WeaponId = w.WeaponId,
            Name = w.Name,
            Description = w.Description,
            Cost = w.Cost,
            Damage = w.Damage,
            IconKey = w.IconKey
        }).ToList();
    }

    public async Task<AttackResponse> PurchaseAttackAsync(int userId, AttackRequest request)
    {
        // 1. Validate weapon exists and is active
        var weapon = await _weaponRepository.GetByIdAsync(request.WeaponId);
        if (weapon == null || !weapon.IsActive)
        {
            _logger.LogWarning("Attack rejected for user {UserId}: weapon {WeaponId} not found or inactive.",
                userId, request.WeaponId);
            return new AttackResponse
            {
                Success = false,
                Message = "Weapon not found or is no longer available."
            };
        }

        // 2. Atomic coin deduction — returns false if insufficient funds
        var deducted = await _walletRepository.TryDeductCoinsAsync(userId, weapon.Cost);
        if (!deducted)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);
            _logger.LogInformation("Attack rejected for user {UserId}: insufficient coins (has {Balance}, needs {Cost}).",
                userId, wallet?.Coins ?? 0, weapon.Cost);
            return new AttackResponse
            {
                Success = false,
                CurrentBalance = wallet?.Coins ?? 0,
                Message = "Insufficient coins to purchase this weapon."
            };
        }

        // 3. Record the attack
        var attackLog = new AttackLog
        {
            UserId = userId,
            WeaponId = weapon.WeaponId,
            MatchId = request.MatchId,
            TeamId = request.TeamId,
            CoinsSpent = weapon.Cost,
            DamageDealt = weapon.Damage
        };

        var attackId = await _attackRepository.RecordAttackAsync(attackLog);

        // 4. Retrieve updated balance
        var updatedWallet = await _walletRepository.GetByUserIdAsync(userId);
        var newBalance = updatedWallet?.Coins ?? 0;

        // 5. Record coin transaction audit entry
        if (updatedWallet != null)
        {
            try
            {
                await _coinTransactionRepository.RecordTransactionAsync(
                    userId, updatedWallet.WalletId, -weapon.Cost, "WEAPON_PURCHASE", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record coin transaction for user {UserId}, attack {AttackId}.", userId, attackId);
            }
        }

        _logger.LogInformation(
            "Attack submitted: user {UserId} used weapon {WeaponName} (cost {Cost}) for team {TeamId} in match {MatchId}. New balance: {Balance}.",
            userId, weapon.Name, weapon.Cost, request.TeamId, request.MatchId, newBalance);

        return new AttackResponse
        {
            Success = true,
            AttackId = attackId,
            CoinsSpent = weapon.Cost,
            CurrentBalance = newBalance,
            DamageDealt = weapon.Damage,
            Message = $"{weapon.Name} attack launched! Dealt {weapon.Damage} damage."
        };
    }
}
