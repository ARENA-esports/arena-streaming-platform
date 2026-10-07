using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BattleEconomyService.Configuration;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using BattleEconomyService.Services;
using BattleEconomyService.WebSockets;
using Xunit;

namespace BattleEconomyService.Tests;

public class WeaponShopServiceTests
{
    private readonly Mock<IWeaponRepository> _weaponRepoMock;
    private readonly Mock<IAttackRepository> _attackRepoMock;
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<ICoinTransactionRepository> _coinTransactionRepoMock;
    private readonly Mock<IBattleBarRepository> _battleBarRepoMock;
    private readonly Mock<IBattleRoundRepository> _battleRoundRepoMock;
    private readonly Mock<IBattleWebSocketManager> _webSocketManagerMock;
    private readonly Mock<ILogger<WeaponShopService>> _loggerMock;
    private readonly WeaponShopService _service;

    public WeaponShopServiceTests()
    {
        _weaponRepoMock = new Mock<IWeaponRepository>();
        _attackRepoMock = new Mock<IAttackRepository>();
        _walletRepoMock = new Mock<IWalletRepository>();
        _coinTransactionRepoMock = new Mock<ICoinTransactionRepository>();
        _battleBarRepoMock = new Mock<IBattleBarRepository>();
        _battleRoundRepoMock = new Mock<IBattleRoundRepository>();
        _webSocketManagerMock = new Mock<IBattleWebSocketManager>();
        _loggerMock = new Mock<ILogger<WeaponShopService>>();

        _battleRoundRepoMock.Setup(r => r.GetOrCreateActiveRoundAsync(It.IsAny<int>(), It.IsAny<long>()))
            .ReturnsAsync((int matchId, long targetDamage) => new BattleRound
            {
                RoundId = 1,
                MatchId = matchId,
                RoundNumber = 1,
                TargetDamage = targetDamage,
                RoundActive = true
            });

        _service = new WeaponShopService(
            _weaponRepoMock.Object,
            _attackRepoMock.Object,
            _walletRepoMock.Object,
            _coinTransactionRepoMock.Object,
            _battleBarRepoMock.Object,
            _webSocketManagerMock.Object,
            _loggerMock.Object,
            _battleRoundRepoMock.Object,
            Options.Create(new EconomyOptions { RoundTargetDamage = 100 }));
    }

    [Fact]
    public async Task GetWeaponsAsync_ReturnsMappedWeaponDtos()
    {
        // Arrange
        var weapons = new List<Weapon>
        {
            new() { WeaponId = 1, Name = "Sword", Description = "Sharp blade", Cost = 15, Damage = 2, IconKey = "sword", IsActive = true },
            new() { WeaponId = 2, Name = "Bow", Description = "Long range", Cost = 30, Damage = 4, IconKey = "bow", IsActive = true }
        };
        _weaponRepoMock.Setup(r => r.GetActiveWeaponsAsync()).ReturnsAsync(weapons);

        // Act
        var result = await _service.GetWeaponsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Sword", result[0].Name);
        Assert.Equal(15, result[0].Cost);
        Assert.Equal(2, result[0].Damage);
        Assert.Equal("sword", result[0].IconKey);
        Assert.Equal("Sharp blade", result[0].Description);
    }

    [Fact]
    public async Task PurchaseAttackAsync_WeaponNotFound_ReturnsFailure()
    {
        // Arrange
        _weaponRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Weapon?)null);
        var request = new AttackRequest { WeaponId = 99, MatchId = 1, TeamId = 1 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        _walletRepoMock.Verify(w => w.TryDeductCoinsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _battleBarRepoMock.Verify(b => b.ApplyDamageAtomicAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PurchaseAttackAsync_WeaponInactive_ReturnsFailure()
    {
        // Arrange
        var inactiveWeapon = new Weapon { WeaponId = 5, Name = "Inactive Gun", Cost = 50, IsActive = false };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(inactiveWeapon);
        var request = new AttackRequest { WeaponId = 5, MatchId = 1, TeamId = 1 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("available", result.Message, StringComparison.OrdinalIgnoreCase);
        _walletRepoMock.Verify(w => w.TryDeductCoinsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _battleBarRepoMock.Verify(b => b.ApplyDamageAtomicAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PurchaseAttackAsync_InsufficientCoins_ReturnsFailureWithBalance()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 1, Name = "Hammer", Cost = 50, Damage = 6, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 50)).ReturnsAsync(false);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { UserId = 10, Coins = 20 });

        var request = new AttackRequest { WeaponId = 1, MatchId = 1, TeamId = 2 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(20, result.CurrentBalance);
        Assert.Contains("Insufficient coins", result.Message, StringComparison.OrdinalIgnoreCase);
        _attackRepoMock.Verify(a => a.RecordAttackAsync(It.IsAny<AttackLog>()), Times.Never);
        _battleBarRepoMock.Verify(b => b.ApplyDamageAtomicAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PurchaseAttackAsync_Success_DeductsCoinsRecordsAttackAndUpdatesBattleBar()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 1, Name = "Hammer", Cost = 50, Damage = 6, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 50)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 50 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(12345L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 2, 6)).ReturnsAsync(106L);
        _battleBarRepoMock.Setup(b => b.GetBarsForMatchAsync(101))
            .ReturnsAsync(new List<BattleBar> { new() { MatchId = 101, TeamId = 2, TotalDamage = 106 } });

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 2 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(12345L, result.AttackId);
        Assert.Equal(50, result.CoinsSpent);
        Assert.Equal(50, result.CurrentBalance);
        Assert.Equal(6, result.DamageDealt);
        Assert.Equal(106L, result.TeamTotalDamage);
        Assert.Contains("Hammer attack launched", result.Message);

        _attackRepoMock.Verify(a => a.RecordAttackAsync(It.Is<AttackLog>(log =>
            log.UserId == 10 &&
            log.WeaponId == 1 &&
            log.MatchId == 101 &&
            log.TeamId == 2 &&
            log.CoinsSpent == 50 &&
            log.DamageDealt == 6)), Times.Once);

        _battleBarRepoMock.Verify(b => b.ApplyDamageAtomicAsync(101, 2, 6), Times.Once);

        _webSocketManagerMock.Verify(ws => ws.BroadcastToMatchAsync(
            101,
            It.Is<BattleBarBroadcastMessage>(m =>
                m.MatchId == 101 &&
                m.Type == "battle_bar_update" &&
                m.LatestAttack != null &&
                m.LatestAttack.Damage == 6),
            It.IsAny<CancellationToken>()), Times.Once);

        _coinTransactionRepoMock.Verify(c => c.RecordTransactionAsync(
            10, 77, -50, "WEAPON_PURCHASE", null), Times.Once);
    }

    [Fact]
    public async Task PurchaseAttackAsync_WebSocketBroadcastFails_StillReturnsSuccess()
    {
        // Arrange — WebSocket broadcast fails, but attack must still succeed (fault isolation)
        var weapon = new Weapon { WeaponId = 1, Name = "Hammer", Cost = 50, Damage = 6, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 50)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 50 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(12345L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 2, 6)).ReturnsAsync(106L);
        _webSocketManagerMock.Setup(ws => ws.BroadcastToMatchAsync(It.IsAny<int>(), It.IsAny<BattleBarBroadcastMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Socket buffer overflow"));

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 2 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert — attack succeeds despite WebSocket broadcast failure
        Assert.True(result.Success);
        Assert.Equal(12345L, result.AttackId);
        Assert.Equal(106L, result.TeamTotalDamage);
    }

    [Fact]
    public async Task PurchaseAttackAsync_BattleBarUpdateFails_StillReturnsSuccess()
    {
        // Arrange — battle bar throws, but the attack and coin deduction should still succeed
        var weapon = new Weapon { WeaponId = 1, Name = "Knife", Cost = 10, Damage = 1, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 10)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 90 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(999L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB connection dropped for battle bar"));

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 2 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert — attack still succeeds, TeamTotalDamage defaults to 0
        Assert.True(result.Success);
        Assert.Equal(999L, result.AttackId);
        Assert.Equal(90, result.CurrentBalance);
        Assert.Equal(0L, result.TeamTotalDamage);
    }

    [Fact]
    public async Task PurchaseAttackAsync_TransactionAuditFails_StillReturnsSuccess()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 1, Name = "Knife", Cost = 10, Damage = 1, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 10)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 90 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(888L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 2, 1)).ReturnsAsync(50L);
        _coinTransactionRepoMock.Setup(c => c.RecordTransactionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ThrowsAsync(new Exception("DB connection dropped for audit log"));

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 2 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(888L, result.AttackId);
        Assert.Equal(90, result.CurrentBalance);
    }

    [Fact]
    public async Task GetBarsForMatchAsync_DelegatesToRepository()
    {
        // Arrange
        var bars = new List<BattleBar>
        {
            new() { BarId = 1, MatchId = 5, TeamId = 1, TotalDamage = 100 },
            new() { BarId = 2, MatchId = 5, TeamId = 2, TotalDamage = 250 }
        };
        _battleBarRepoMock.Setup(b => b.GetBarsForMatchAsync(5)).ReturnsAsync(bars);

        // Act
        var result = await _service.GetBarsForMatchAsync(5);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[0].TotalDamage);
        Assert.Equal(250, result[1].TotalDamage);
        _battleBarRepoMock.Verify(b => b.GetBarsForMatchAsync(5), Times.Once);
    }

    [Fact]
    public async Task PurchaseAttackAsync_DamageReachesTargetDamage_FlipsRoundActiveAndTriggersReset()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 2, Name = "Bow", Cost = 30, Damage = 15, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 30)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 70 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(500L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 1, 15)).ReturnsAsync(100L);
        _battleBarRepoMock.Setup(b => b.GetBarsForMatchAsync(101)).ReturnsAsync(new List<BattleBar>
        {
            new() { BarId = 1, MatchId = 101, TeamId = 1, TotalDamage = 0 },
            new() { BarId = 2, MatchId = 101, TeamId = 2, TotalDamage = 0 }
        });

        // Winning flip
        _battleRoundRepoMock.Setup(r => r.TryFlipRoundActiveAsync(1, 1)).ReturnsAsync(true);
        _battleRoundRepoMock.Setup(r => r.ResetBarsAndStartNextRoundAsync(101, 2, 100))
            .ReturnsAsync(new BattleRound { RoundId = 2, MatchId = 101, RoundNumber = 2, TargetDamage = 100, RoundActive = true });

        var request = new AttackRequest { WeaponId = 2, MatchId = 101, TeamId = 1 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.RoundEnded);
        Assert.Equal(1, result.WinningTeamId);
        Assert.Equal(2, result.RoundNumber);
        Assert.Equal(0L, result.TeamTotalDamage);
        Assert.Contains("delivered the final blow", result.Message);

        _battleRoundRepoMock.Verify(r => r.TryFlipRoundActiveAsync(1, 1), Times.Once);
        _battleRoundRepoMock.Verify(r => r.ResetBarsAndStartNextRoundAsync(101, 2, 100), Times.Once);
        _webSocketManagerMock.Verify(w => w.BroadcastToMatchAsync(101, It.Is<BattleBarBroadcastMessage>(m =>
            m.Type == "round_reset" &&
            m.RoundEnded == true &&
            m.WinningTeamId == 1 &&
            m.RoundNumber == 2
        ), default), Times.Once);
    }

    [Fact]
    public async Task PurchaseAttackAsync_ConcurrentAttackReachesTarget_FlipReturnsFalse_SkipsReset()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 2, Name = "Bow", Cost = 30, Damage = 15, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 30)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 70 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(501L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 1, 15)).ReturnsAsync(100L);
        _battleBarRepoMock.Setup(b => b.GetBarsForMatchAsync(101)).ReturnsAsync(new List<BattleBar>
        {
            new() { BarId = 1, MatchId = 101, TeamId = 1, TotalDamage = 100 }
        });

        // Another concurrent thread already flipped the flag, so TryFlipRoundActiveAsync returns false
        _battleRoundRepoMock.Setup(r => r.TryFlipRoundActiveAsync(1, 1)).ReturnsAsync(false);

        var request = new AttackRequest { WeaponId = 2, MatchId = 101, TeamId = 1 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert - round-end does NOT fire twice
        Assert.True(result.Success);
        Assert.False(result.RoundEnded);
        Assert.Null(result.WinningTeamId);
        Assert.Equal(100L, result.TeamTotalDamage);

        _battleRoundRepoMock.Verify(r => r.TryFlipRoundActiveAsync(1, 1), Times.Once);
        // CRITICAL: ResetBarsAndStartNextRoundAsync must NEVER be called if flip returns false
        _battleRoundRepoMock.Verify(r => r.ResetBarsAndStartNextRoundAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>()), Times.Never);
        _webSocketManagerMock.Verify(w => w.BroadcastToMatchAsync(101, It.Is<BattleBarBroadcastMessage>(m =>
            m.Type == "battle_bar_update" &&
            m.RoundEnded == false
        ), default), Times.Once);
    }

    [Fact]
    public async Task PurchaseAttackAsync_DamageBelowTargetDamage_DoesNotAttemptFlip()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 1, Name = "Sword", Cost = 15, Damage = 5, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 15)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 85 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(502L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 1, 5)).ReturnsAsync(35L);
        _battleBarRepoMock.Setup(b => b.GetBarsForMatchAsync(101)).ReturnsAsync(new List<BattleBar>
        {
            new() { BarId = 1, MatchId = 101, TeamId = 1, TotalDamage = 35 }
        });

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 1 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.RoundEnded);
        Assert.Equal(35L, result.TeamTotalDamage);
        _battleRoundRepoMock.Verify(r => r.TryFlipRoundActiveAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _battleRoundRepoMock.Verify(r => r.ResetBarsAndStartNextRoundAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task PurchaseAttackAsync_RoundRepositoryThrows_ContinuesGracefullyWithoutFailingAttack()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 1, Name = "Sword", Cost = 15, Damage = 5, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 15)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 85 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(503L);
        _battleBarRepoMock.Setup(b => b.ApplyDamageAtomicAsync(101, 1, 5)).ReturnsAsync(50L);

        // Round repository throws an unexpected database exception
        _battleRoundRepoMock.Setup(r => r.GetOrCreateActiveRoundAsync(It.IsAny<int>(), It.IsAny<long>()))
            .ThrowsAsync(new Exception("Round database table unavailable"));

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 1 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert - Attack still succeeds even if round check errors
        Assert.True(result.Success);
        Assert.Equal(503L, result.AttackId);
        Assert.False(result.RoundEnded);
    }

    [Fact]
    public async Task GetRoundHistoryAsync_ReturnsMappedHistoryDtosWithParsedBars()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var rounds = new List<BattleRound>
        {
            new()
            {
                RoundId = 1,
                MatchId = 101,
                RoundNumber = 1,
                TargetDamage = 100,
                WinningTeamId = 1,
                RoundActive = false,
                FinalBarState = "[{\"TeamId\":1,\"TotalDamage\":100},{\"TeamId\":2,\"TotalDamage\":45}]",
                CreatedAt = now.AddMinutes(-5),
                EndedAt = now
            }
        };

        _battleRoundRepoMock.Setup(r => r.GetRoundHistoryAsync(101)).ReturnsAsync(rounds);

        // Act
        var result = await _service.GetRoundHistoryAsync(101);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].RoundId);
        Assert.Equal(101, result[0].MatchId);
        Assert.Equal(1, result[0].RoundNumber);
        Assert.Equal(1, result[0].WinningTeamId);
        Assert.Equal(100, result[0].TargetDamage);
        Assert.Equal(2, result[0].FinalBarState.Count);
        Assert.Equal(1, result[0].FinalBarState[0].TeamId);
        Assert.Equal(100, result[0].FinalBarState[0].TotalDamage);
        Assert.Equal(2, result[0].FinalBarState[1].TeamId);
        Assert.Equal(45, result[0].FinalBarState[1].TotalDamage);
    }

    [Fact]
    public async Task GetRoundHistoryAsync_WhenNoRounds_ReturnsEmptyList()
    {
        // Arrange
        _battleRoundRepoMock.Setup(r => r.GetRoundHistoryAsync(999)).ReturnsAsync(new List<BattleRound>());

        // Act
        var result = await _service.GetRoundHistoryAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRoundHistoryAsync_WhenFinalBarStateMalformed_GracefullyFallsBackToEmptyBars()
    {
        // Arrange
        var rounds = new List<BattleRound>
        {
            new()
            {
                RoundId = 2,
                MatchId = 101,
                RoundNumber = 1,
                TargetDamage = 100,
                WinningTeamId = 2,
                RoundActive = false,
                FinalBarState = "MALFORMED_NOT_A_JSON",
                CreatedAt = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow
            }
        };

        _battleRoundRepoMock.Setup(r => r.GetRoundHistoryAsync(101)).ReturnsAsync(rounds);

        // Act
        var result = await _service.GetRoundHistoryAsync(101);

        // Assert
        Assert.Single(result);
        Assert.Empty(result[0].FinalBarState);
    }
}
