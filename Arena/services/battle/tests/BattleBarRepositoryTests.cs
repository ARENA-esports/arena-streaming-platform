using Microsoft.Extensions.Configuration;
using MySqlConnector;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using Xunit;

namespace BattleEconomyService.Tests;

public class BattleBarRepositoryTests
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
        var exception = Record.Exception(() => new BattleBarRepository(configuration));
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
        var ex = Assert.Throws<InvalidOperationException>(() => new BattleBarRepository(configuration));
        Assert.Contains("DefaultConnection", ex.Message);
    }

    [Fact]
    public void BattleBar_Model_InitializesAndSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var bar = new BattleBar
        {
            BarId = 1,
            MatchId = 42,
            TeamId = 2,
            TotalDamage = 350,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        Assert.Equal(1, bar.BarId);
        Assert.Equal(42, bar.MatchId);
        Assert.Equal(2, bar.TeamId);
        Assert.Equal(350, bar.TotalDamage);
        Assert.Equal(now, bar.CreatedAt);
        Assert.Equal(now, bar.UpdatedAt);
    }

    [Fact]
    public async Task ApplyDamageAtomicAsync_ConcurrentAttacks_NoLostUpdates()
    {
        // Check if local test MySQL is available
        if (!await CanConnectToTestDatabaseAsync())
        {
            // Skip if database is not reachable in current environment
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", TestConnectionString }
            })
            .Build();

        var repo = new BattleBarRepository(configuration);

        // Pick a unique match ID for this test run to avoid conflict
        var testMatchId = 90000 + Random.Shared.Next(1, 9999);
        var testTeamId = 1;
        const int concurrentAttackCount = 20;
        const int damagePerAttack = 5;
        const int expectedTotalDamage = concurrentAttackCount * damagePerAttack;

        try
        {
            // Launch concurrent attacks simultaneously to test race condition resilience
            var attackTasks = Enumerable.Range(0, concurrentAttackCount)
                .Select(_ => Task.Run(() => repo.ApplyDamageAtomicAsync(testMatchId, testTeamId, damagePerAttack)))
                .ToArray();

            await Task.WhenAll(attackTasks);

            // Fetch final bar totals
            var bars = await repo.GetBarsForMatchAsync(testMatchId);
            var teamBar = bars.FirstOrDefault(b => b.TeamId == testTeamId);

            Assert.NotNull(teamBar);
            Assert.Equal(expectedTotalDamage, teamBar.TotalDamage);
        }
        finally
        {
            // Clean up test data
            await CleanupTestDataAsync(testMatchId);
        }
    }

    [Fact]
    public async Task GetBarsForMatchAsync_ReturnsBarsForBothTeams()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", TestConnectionString }
            })
            .Build();

        var repo = new BattleBarRepository(configuration);
        var testMatchId = 80000 + Random.Shared.Next(1, 9999);

        try
        {
            await repo.ApplyDamageAtomicAsync(testMatchId, 1, 10);
            await repo.ApplyDamageAtomicAsync(testMatchId, 2, 25);

            var bars = await repo.GetBarsForMatchAsync(testMatchId);

            Assert.Equal(2, bars.Count);
            Assert.Contains(bars, b => b.TeamId == 1 && b.TotalDamage == 10);
            Assert.Contains(bars, b => b.TeamId == 2 && b.TotalDamage == 25);
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
        }
    }

    private static async Task<bool> CanConnectToTestDatabaseAsync()
    {
        try
        {
            using var connection = new MySqlConnection(TestConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task CleanupTestDataAsync(int matchId)
    {
        try
        {
            using var connection = new MySqlConnection(TestConnectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM battle_bars WHERE match_id = @MatchId;", connection);
            cmd.Parameters.AddWithValue("@MatchId", matchId);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
