using BattleEconomyService.Configuration;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using BattleEconomyService.WebSockets;
using Microsoft.Extensions.Options;

namespace BattleEconomyService.Services;

public class WeaponShopService : IWeaponShopService
{
    private readonly IWeaponRepository _weaponRepository;
    private readonly IAttackRepository _attackRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly IBattleBarRepository _battleBarRepository;
    private readonly IBattleRoundRepository? _battleRoundRepository;
    private readonly IBattleWebSocketManager _webSocketManager;
    private readonly EconomyOptions _economyOptions;
    private readonly ILogger<WeaponShopService> _logger;

    public WeaponShopService(
        IWeaponRepository weaponRepository,
        IAttackRepository attackRepository,
        IWalletRepository walletRepository,
        ICoinTransactionRepository coinTransactionRepository,
        IBattleBarRepository battleBarRepository,
        IBattleWebSocketManager webSocketManager,
        ILogger<WeaponShopService> logger,
        IBattleRoundRepository? battleRoundRepository = null,
        IOptions<EconomyOptions>? economyOptions = null)
    {
        _weaponRepository = weaponRepository;
        _attackRepository = attackRepository;
        _walletRepository = walletRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _battleBarRepository = battleBarRepository;
        _webSocketManager = webSocketManager;
        _logger = logger;
        _battleRoundRepository = battleRoundRepository;
        _economyOptions = economyOptions?.Value ?? new EconomyOptions();
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

        // 5. Round-End Atomic Check-and-Flip & Reset (SCRUM-122)
        //    When a team's cumulative damage reaches or exceeds the round target damage (100%):
        //    Execute an atomic UPDATE check-and-flip (WHERE round_id = @RoundId AND round_active = TRUE).
        //    InnoDB row locking ensures exactly one process gets RowsAffected = 1 and resets bars + starts next round.
        //    All concurrent requests get RowsAffected = 0 and skip reset, ensuring round-end never fires twice.
        bool roundEnded = false;
        int? winningTeamId = null;
        int activeRoundNumber = 1;

        if (_battleRoundRepository != null)
        {
            try
            {
                var activeRound = await _battleRoundRepository.GetOrCreateActiveRoundAsync(
                    request.MatchId, _economyOptions.RoundTargetDamage);
                activeRoundNumber = activeRound.RoundNumber;

                if (teamTotalDamage >= activeRound.TargetDamage)
                {
                    var successfullyFlipped = await _battleRoundRepository.TryFlipRoundActiveAsync(
                        activeRound.RoundId, request.TeamId);

                    if (successfullyFlipped)
                    {
                        roundEnded = true;
                        winningTeamId = request.TeamId;

                        _logger.LogInformation(
                            "Round {RoundNumber} won by Team {TeamId} in match {MatchId} (Damage: {Damage}/{Target}). Triggering atomic reset.",
                            activeRound.RoundNumber, request.TeamId, request.MatchId, teamTotalDamage, activeRound.TargetDamage);

                        var nextRound = await _battleRoundRepository.ResetBarsAndStartNextRoundAsync(
                            request.MatchId, activeRound.RoundNumber + 1, activeRound.TargetDamage);

                        activeRoundNumber = nextRound.RoundNumber;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Round {RoundId} in match {MatchId} was already flipped and reset by a concurrent process. Skipping duplicate reset.",
                            activeRound.RoundId, request.MatchId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform round-end check or reset for match {MatchId}, attack {AttackId}.",
                    request.MatchId, attackId);
            }
        }

        // 6. Broadcast real-time battle bar update to all connected viewers (SCRUM-121 / SCRUM-122)
        try
        {
            var matchBars = await _battleBarRepository.GetBarsForMatchAsync(request.MatchId) ?? new List<BattleBar>();
            var broadcast = new BattleBarBroadcastMessage
            {
                Type = roundEnded ? "round_reset" : "battle_bar_update",
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
                RoundNumber = activeRoundNumber,
                RoundEnded = roundEnded,
                WinningTeamId = winningTeamId,
                Timestamp = DateTime.UtcNow
            };

            await _webSocketManager.BroadcastToMatchAsync(request.MatchId, broadcast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast battle bar update for match {MatchId}, attack {AttackId}.",
                request.MatchId, attackId);
        }

        // 7. Retrieve updated balance
        var updatedWallet = await _walletRepository.GetByUserIdAsync(userId);
        var newBalance = updatedWallet?.Coins ?? 0;

        // 8. Record coin transaction audit entry
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

        var attackMessage = roundEnded
            ? $"{weapon.Name} attack delivered the final blow! Team {winningTeamId} won Round {activeRoundNumber - 1}! Round reset to Round {activeRoundNumber}."
            : $"{weapon.Name} attack launched! Dealt {weapon.Damage} damage.";

        _logger.LogInformation(
            "Attack submitted: user {UserId} used weapon {WeaponName} (cost {Cost}) for team {TeamId} in match {MatchId}. Damage: {Damage}. Bar total: {BarTotal}. RoundEnded: {RoundEnded}. New balance: {Balance}.",
            userId, weapon.Name, weapon.Cost, request.TeamId, request.MatchId, weapon.Damage, teamTotalDamage, roundEnded, newBalance);

        return new AttackResponse
        {
            Success = true,
            AttackId = attackId,
            CoinsSpent = weapon.Cost,
            CurrentBalance = newBalance,
            DamageDealt = weapon.Damage,
            TeamTotalDamage = roundEnded ? 0 : teamTotalDamage,
            RoundEnded = roundEnded,
            WinningTeamId = winningTeamId,
            RoundNumber = activeRoundNumber,
            Message = attackMessage
        };
    }

    public async Task<List<BattleBar>> GetBarsForMatchAsync(int matchId)
    {
        return await _battleBarRepository.GetBarsForMatchAsync(matchId);
    }
}
