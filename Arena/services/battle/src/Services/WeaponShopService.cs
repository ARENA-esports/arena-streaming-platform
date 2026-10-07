using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using BattleEconomyService.WebSockets;

namespace BattleEconomyService.Services;

public class WeaponShopService : IWeaponShopService
{
    private readonly IWeaponRepository _weaponRepository;
    private readonly IAttackRepository _attackRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly IBattleBarRepository _battleBarRepository;
    private readonly IBattleWebSocketManager _webSocketManager;
    private readonly ILogger<WeaponShopService> _logger;

    public WeaponShopService(
        IWeaponRepository weaponRepository,
        IAttackRepository attackRepository,
        IWalletRepository walletRepository,
        ICoinTransactionRepository coinTransactionRepository,
        IBattleBarRepository battleBarRepository,
        IBattleWebSocketManager webSocketManager,
        ILogger<WeaponShopService> logger)
    {
        _weaponRepository = weaponRepository;
        _attackRepository = attackRepository;
        _walletRepository = walletRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _battleBarRepository = battleBarRepository;
        _webSocketManager = webSocketManager;
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

        // 4. Atomically update the battle bar (SCRUM-120)
        //    Uses INSERT ... ON DUPLICATE KEY UPDATE total_damage = total_damage + @Damage.
        //    InnoDB row-level locking ensures no lost updates under concurrent load.
        long teamTotalDamage = 0;
        try
        {
            teamTotalDamage = await _battleBarRepository.ApplyDamageAtomicAsync(
                request.MatchId, request.TeamId, weapon.Damage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update battle bar for match {MatchId}, team {TeamId}. Attack {AttackId} was still recorded.",
                request.MatchId, request.TeamId, attackId);
        }

        // 5. Broadcast real-time battle bar update to all connected viewers (SCRUM-121)
        try
        {
            var matchBars = await _battleBarRepository.GetBarsForMatchAsync(request.MatchId) ?? new List<BattleBar>();
            var broadcast = new BattleBarBroadcastMessage
            {
                Type = "battle_bar_update",
                MatchId = request.MatchId,
                Bars = matchBars.Select(b => new BattleBarDto
                {
                    TeamId = b.TeamId,
                    TotalDamage = b.TotalDamage
                }).ToList(),
                LatestAttack = new AttackEventDto
                {
                    TeamId = request.TeamId,
                    Damage = weapon.Damage,
                    WeaponName = weapon.Name,
                    WeaponId = weapon.WeaponId
                },
                Timestamp = DateTime.UtcNow
            };

            await _webSocketManager.BroadcastToMatchAsync(request.MatchId, broadcast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast battle bar update for match {MatchId}, attack {AttackId}.",
                request.MatchId, attackId);
        }

        // 6. Retrieve updated balance
        var updatedWallet = await _walletRepository.GetByUserIdAsync(userId);
        var newBalance = updatedWallet?.Coins ?? 0;

        // 6. Record coin transaction audit entry
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
            "Attack submitted: user {UserId} used weapon {WeaponName} (cost {Cost}) for team {TeamId} in match {MatchId}. Damage: {Damage}. Bar total: {BarTotal}. New balance: {Balance}.",
            userId, weapon.Name, weapon.Cost, request.TeamId, request.MatchId, weapon.Damage, teamTotalDamage, newBalance);

        return new AttackResponse
        {
            Success = true,
            AttackId = attackId,
            CoinsSpent = weapon.Cost,
            CurrentBalance = newBalance,
            DamageDealt = weapon.Damage,
            TeamTotalDamage = teamTotalDamage,
            Message = $"{weapon.Name} attack launched! Dealt {weapon.Damage} damage."
        };
    }

    public async Task<List<BattleBar>> GetBarsForMatchAsync(int matchId)
    {
        return await _battleBarRepository.GetBarsForMatchAsync(matchId);
    }
}
