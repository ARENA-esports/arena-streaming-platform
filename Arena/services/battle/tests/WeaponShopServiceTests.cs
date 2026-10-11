using Microsoft.Extensions.Logging;
using Moq;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using BattleEconomyService.Services;
using Xunit;

namespace BattleEconomyService.Tests;

public class WeaponShopServiceTests
{
    private readonly Mock<IWeaponRepository> _weaponRepoMock;
    private readonly Mock<IAttackRepository> _attackRepoMock;
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<ICoinTransactionRepository> _coinTransactionRepoMock;
    private readonly Mock<ILogger<WeaponShopService>> _loggerMock;
    private readonly WeaponShopService _service;

    public WeaponShopServiceTests()
    {
        _weaponRepoMock = new Mock<IWeaponRepository>();
        _attackRepoMock = new Mock<IAttackRepository>();
        _walletRepoMock = new Mock<IWalletRepository>();
        _coinTransactionRepoMock = new Mock<ICoinTransactionRepository>();
        _loggerMock = new Mock<ILogger<WeaponShopService>>();

        _service = new WeaponShopService(
            _weaponRepoMock.Object,
            _attackRepoMock.Object,
            _walletRepoMock.Object,
            _coinTransactionRepoMock.Object,
            _loggerMock.Object);
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
    }

    [Fact]
    public async Task PurchaseAttackAsync_Success_DeductsCoinsAndRecordsAttackAndTransaction()
    {
        // Arrange
        var weapon = new Weapon { WeaponId = 1, Name = "Hammer", Cost = 50, Damage = 6, IsActive = true };
        _weaponRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(weapon);
        _walletRepoMock.Setup(w => w.TryDeductCoinsAsync(10, 50)).ReturnsAsync(true);
        _walletRepoMock.Setup(w => w.GetByUserIdAsync(10)).ReturnsAsync(new Wallet { WalletId = 77, UserId = 10, Coins = 50 });
        _attackRepoMock.Setup(a => a.RecordAttackAsync(It.IsAny<AttackLog>())).ReturnsAsync(12345L);

        var request = new AttackRequest { WeaponId = 1, MatchId = 101, TeamId = 2 };

        // Act
        var result = await _service.PurchaseAttackAsync(10, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(12345L, result.AttackId);
        Assert.Equal(50, result.CoinsSpent);
        Assert.Equal(50, result.CurrentBalance);
        Assert.Equal(6, result.DamageDealt);
        Assert.Contains("Hammer attack launched", result.Message);

        _attackRepoMock.Verify(a => a.RecordAttackAsync(It.Is<AttackLog>(log =>
            log.UserId == 10 &&
            log.WeaponId == 1 &&
            log.MatchId == 101 &&
            log.TeamId == 2 &&
            log.CoinsSpent == 50 &&
            log.DamageDealt == 6)), Times.Once);

        _coinTransactionRepoMock.Verify(c => c.RecordTransactionAsync(
            10, 77, -50, "WEAPON_PURCHASE", null), Times.Once);
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
}
