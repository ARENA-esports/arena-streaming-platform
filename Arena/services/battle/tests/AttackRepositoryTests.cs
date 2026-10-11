using Microsoft.Extensions.Configuration;
using MySqlConnector;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using Xunit;

namespace BattleEconomyService.Tests;

public class AttackRepositoryTests
{
    private const string TestConnectionString =
        "Server=localhost;Port=3310;Database=arena_battle_db;Uid=arena;Pwd=arena_password;";

    [Fact]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", TestConnectionString }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act & Assert
        var exception = Record.Exception(() => new AttackRepository(configuration));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithMissingConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new AttackRepository(configuration));
    }

    [Fact]
    public async Task ExecuteAttackTransactionAsync_WhenInsufficientCoins_ReturnsInsufficientCoins()
    {
        var config = CreateTestConfiguration();
        var repo = new AttackRepository(config);
        const int testUserId = 888801;

        await EnsureUserWalletAsync(testUserId, coins: 20);

        try
        {
            var attackLog = new AttackLog
            {
                UserId = testUserId,
                WeaponId = 1,
                MatchId = 9991,
                TeamId = 1,
                CoinsSpent = 50,
                DamageDealt = 10
            };

            var result = await repo.ExecuteAttackTransactionAsync(attackLog, weaponCost: 50);

            Assert.False(result.Success);
            Assert.True(result.InsufficientFunds);
            Assert.Equal(20, result.RemainingBalance);
        }
        finally
        {
            await CleanupUserAsync(testUserId);
        }
    }

    [Fact]
    public async Task ExecuteAttackTransactionAsync_WhenSufficientCoins_ExecutesAtomically()
    {
        var config = CreateTestConfiguration();
        var repo = new AttackRepository(config);
        const int testUserId = 888802;

        await EnsureUserWalletAsync(testUserId, coins: 100);

        try
        {
            var attackLog = new AttackLog
            {
                UserId = testUserId,
                WeaponId = 1,
                MatchId = 9992,
                TeamId = 2,
                CoinsSpent = 40,
                DamageDealt = 8
            };

            var result = await repo.ExecuteAttackTransactionAsync(attackLog, weaponCost: 40);

            Assert.True(result.Success);
            Assert.True(result.AttackId > 0);
            Assert.Equal(60, result.RemainingBalance);
            Assert.True(result.WalletId > 0);

            // Verify coins in database
            var currentCoins = await GetUserCoinsAsync(testUserId);
            Assert.Equal(60, currentCoins);
        }
        finally
        {
            await CleanupUserAsync(testUserId);
        }
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", TestConnectionString }
            })
            .Build();
    }

    private static async Task EnsureUserWalletAsync(int userId, int coins)
    {
        using var connection = new MySqlConnection(TestConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO wallets (user_id, coins)
            VALUES (@UserId, @Coins)
            ON DUPLICATE KEY UPDATE coins = @Coins;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Coins", coins);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> GetUserCoinsAsync(int userId)
    {
        using var connection = new MySqlConnection(TestConnectionString);
        await connection.OpenAsync();

        const string sql = "SELECT coins FROM wallets WHERE user_id = @UserId LIMIT 1;";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task CleanupUserAsync(int userId)
    {
        using var connection = new MySqlConnection(TestConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            DELETE FROM coin_transactions WHERE user_id = @UserId;
            DELETE FROM attack_log WHERE user_id = @UserId;
            DELETE FROM wallets WHERE user_id = @UserId;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        await command.ExecuteNonQueryAsync();
    }
}
