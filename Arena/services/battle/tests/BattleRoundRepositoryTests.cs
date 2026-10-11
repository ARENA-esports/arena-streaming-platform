using Microsoft.Extensions.Configuration;
using MySqlConnector;
using BattleEconomyService.Models;
using BattleEconomyService.Repositories;
using Xunit;

namespace BattleEconomyService.Tests;

public class BattleRoundRepositoryTests
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
        var exception = Record.Exception(() => new BattleRoundRepository(configuration));
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
        var ex = Assert.Throws<InvalidOperationException>(() => new BattleRoundRepository(configuration));
        Assert.Contains("DefaultConnection", ex.Message);
    }

    [Fact]
    public void BattleRound_Model_InitializesAndSetsPropertiesCorrectly()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var round = new BattleRound
        {
            RoundId = 10,
            MatchId = 42,
            RoundNumber = 3,
            RoundActive = true,
            TargetDamage = 100,
            WinningTeamId = 1,
            FinalBarState = "[{\"teamId\":1,\"totalDamage\":100},{\"teamId\":2,\"totalDamage\":45}]",
            CreatedAt = now.AddMinutes(-10),
            EndedAt = now
        };

        // Assert
        Assert.Equal(10, round.RoundId);
        Assert.Equal(42, round.MatchId);
        Assert.Equal(3, round.RoundNumber);
        Assert.True(round.RoundActive);
        Assert.Equal(100, round.TargetDamage);
        Assert.Equal(1, round.WinningTeamId);
        Assert.Equal("[{\"teamId\":1,\"totalDamage\":100},{\"teamId\":2,\"totalDamage\":45}]", round.FinalBarState);
        Assert.Equal(now.AddMinutes(-10), round.CreatedAt);
        Assert.Equal(now, round.EndedAt);
    }

    [Fact]
    public async Task GetOrCreateActiveRoundAsync_CreatesRoundOneWhenNoneExists()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);
        var testMatchId = 70000 + Random.Shared.Next(1, 9999);

        try
        {
            var round = await repo.GetOrCreateActiveRoundAsync(testMatchId, targetDamage: 100);

            Assert.NotNull(round);
            Assert.Equal(testMatchId, round.MatchId);
            Assert.Equal(1, round.RoundNumber);
            Assert.True(round.RoundActive);
            Assert.Equal(100, round.TargetDamage);
            Assert.Null(round.WinningTeamId);
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
        }
    }

    [Fact]
    public async Task TryFlipRoundActiveAsync_SingleCall_SuccessfullyFlipsRoundActive()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);
        var testMatchId = 71000 + Random.Shared.Next(1, 9999);

        try
        {
            var round = await repo.GetOrCreateActiveRoundAsync(testMatchId, targetDamage: 100);

            // First flip should succeed
            var flipResult1 = await repo.TryFlipRoundActiveAsync(round.RoundId, winningTeamId: 1);
            Assert.True(flipResult1);

            // Second flip on the same inactive round should fail (cannot flip twice)
            var flipResult2 = await repo.TryFlipRoundActiveAsync(round.RoundId, winningTeamId: 2);
            Assert.False(flipResult2);

            // Active round query should now return null as it is no longer active
            var activeRound = await repo.GetActiveRoundAsync(testMatchId);
            Assert.Null(activeRound);
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
        }
    }

    [Fact]
    public async Task TryFlipRoundActiveAsync_ConcurrentThreads_ExactlyOneSucceeds()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);
        var testMatchId = 72000 + Random.Shared.Next(1, 9999);
        const int concurrentAttempts = 20;

        try
        {
            var round = await repo.GetOrCreateActiveRoundAsync(testMatchId, targetDamage: 100);

            // Launch concurrent flip attempts simultaneously across threads
            var flipTasks = Enumerable.Range(0, concurrentAttempts)
                .Select(_ => Task.Run(() => repo.TryFlipRoundActiveAsync(round.RoundId, winningTeamId: 1)))
                .ToArray();

            var results = await Task.WhenAll(flipTasks);

            // Exactly ONE process should successfully flip the active flag
            var successfulFlips = results.Count(r => r);
            var failedFlips = results.Count(r => !r);

            Assert.Equal(1, successfulFlips);
            Assert.Equal(concurrentAttempts - 1, failedFlips);

            // Confirm active round is no longer active in database
            var activeRound = await repo.GetActiveRoundAsync(testMatchId);
            Assert.Null(activeRound);
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
        }
    }

    [Fact]
    public async Task ResetBarsAndStartNextRoundAsync_ResetsBarsToZeroAndCreatesActiveNextRound()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);
        var barRepo = new BattleBarRepository(configuration);
        var testMatchId = 73000 + Random.Shared.Next(1, 9999);

        try
        {
            // Seed Round 1
            var round1 = await repo.GetOrCreateActiveRoundAsync(testMatchId, targetDamage: 100);
            await barRepo.ApplyDamageAtomicAsync(testMatchId, teamId: 1, damage: 100);
            await barRepo.ApplyDamageAtomicAsync(testMatchId, teamId: 2, damage: 45);

            // Flip Round 1
            var flipped = await repo.TryFlipRoundActiveAsync(round1.RoundId, winningTeamId: 1);
            Assert.True(flipped);

            // Reset bars and start Round 2
            var round2 = await repo.ResetBarsAndStartNextRoundAsync(testMatchId, nextRoundNumber: 2, targetDamage: 100);

            Assert.NotNull(round2);
            Assert.Equal(2, round2.RoundNumber);
            Assert.True(round2.RoundActive);
            Assert.Equal(100, round2.TargetDamage);

            // Verify active round query returns Round 2
            var activeRound = await repo.GetActiveRoundAsync(testMatchId);
            Assert.NotNull(activeRound);
            Assert.Equal(round2.RoundId, activeRound.RoundId);
            Assert.Equal(2, activeRound.RoundNumber);
            Assert.True(activeRound.RoundActive);

            // Verify battle bars have been reset to 0
            var bars = await barRepo.GetBarsForMatchAsync(testMatchId);
            Assert.All(bars, bar => Assert.Equal(0, bar.TotalDamage));
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
        }
    }

    [Fact]
    public async Task GetRoundHistoryAsync_WhenNoCompletedRounds_ReturnsEmptyList()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);
        var testMatchId = 74000 + Random.Shared.Next(1, 9999);

        try
        {
            // Only create an active round, no completed round yet
            await repo.GetOrCreateActiveRoundAsync(testMatchId, targetDamage: 100);

            var history = await repo.GetRoundHistoryAsync(testMatchId);

            Assert.NotNull(history);
            Assert.Empty(history);
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
        }
    }

    [Fact]
    public async Task GetRoundHistoryAsync_WhenMatchDoesNotExist_ReturnsEmptyList()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);

        var history = await repo.GetRoundHistoryAsync(999999);

        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetRoundHistoryAsync_WhenCompletedRoundsExist_ReturnsInReverseChronologicalOrderWithFinalBars()
    {
        if (!await CanConnectToTestDatabaseAsync())
        {
            return;
        }

        await EnsureSchemaAsync();

        var configuration = CreateTestConfiguration();
        var repo = new BattleRoundRepository(configuration);
        var barRepo = new BattleBarRepository(configuration);
        var testMatchId = 75000 + Random.Shared.Next(1, 9999);

        try
        {
            // --- Round 1 ---
            var round1 = await repo.GetOrCreateActiveRoundAsync(testMatchId, targetDamage: 100);
            await barRepo.ApplyDamageAtomicAsync(testMatchId, teamId: 1, damage: 100);
            await barRepo.ApplyDamageAtomicAsync(testMatchId, teamId: 2, damage: 45);

            var flipped1 = await repo.TryFlipRoundActiveAsync(round1.RoundId, winningTeamId: 1);
            Assert.True(flipped1);

            // Reset bars and start Round 2 (snapshots Round 1 bars)
            var round2 = await repo.ResetBarsAndStartNextRoundAsync(testMatchId, nextRoundNumber: 2, targetDamage: 100);
            Assert.NotNull(round2);

            // --- Round 2 ---
            await barRepo.ApplyDamageAtomicAsync(testMatchId, teamId: 1, damage: 60);
            await barRepo.ApplyDamageAtomicAsync(testMatchId, teamId: 2, damage: 100);

            var flipped2 = await repo.TryFlipRoundActiveAsync(round2.RoundId, winningTeamId: 2);
            Assert.True(flipped2);

            // Reset bars and start Round 3 (snapshots Round 2 bars)
            var round3 = await repo.ResetBarsAndStartNextRoundAsync(testMatchId, nextRoundNumber: 3, targetDamage: 100);
            Assert.NotNull(round3);

            // Act - Fetch round history
            var history = await repo.GetRoundHistoryAsync(testMatchId);

            // Assert
            Assert.NotNull(history);
            Assert.Equal(2, history.Count);

            // 1. Reverse-chronological order: Round 2 first, then Round 1
            Assert.Equal(2, history[0].RoundNumber);
            Assert.Equal(2, history[0].WinningTeamId);
            Assert.False(history[0].RoundActive);

            Assert.Equal(1, history[1].RoundNumber);
            Assert.Equal(1, history[1].WinningTeamId);
            Assert.False(history[1].RoundActive);

            // 2. Final bar state snapshot preserved
            Assert.NotNull(history[0].FinalBarState);
            Assert.Contains("\"TeamId\":2", history[0].FinalBarState);
            Assert.Contains("100", history[0].FinalBarState);

            Assert.NotNull(history[1].FinalBarState);
            Assert.Contains("\"TeamId\":1", history[1].FinalBarState);
            Assert.Contains("100", history[1].FinalBarState);

            // 3. Timestamps populated
            Assert.NotNull(history[0].EndedAt);
            Assert.NotNull(history[1].EndedAt);
        }
        finally
        {
            await CleanupTestDataAsync(testMatchId);
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

    private static async Task EnsureSchemaAsync()
    {
        try
        {
            using var connection = new MySqlConnection(TestConnectionString);
            await connection.OpenAsync();
            var sql = @"
CREATE TABLE IF NOT EXISTS battle_rounds (
    round_id INT AUTO_INCREMENT PRIMARY KEY,
    match_id INT NOT NULL,
    round_number INT NOT NULL DEFAULT 1,
    round_active BOOLEAN NOT NULL DEFAULT TRUE,
    target_damage INT NOT NULL DEFAULT 100,
    winning_team_id INT NULL,
    final_bar_state JSON NULL,
    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_rounds_match_active (match_id, round_active),
    INDEX idx_rounds_match_number (match_id, round_number)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS battle_bars (
    bar_id INT AUTO_INCREMENT PRIMARY KEY,
    match_id INT NOT NULL,
    team_id INT NOT NULL,
    total_damage INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_match_team (match_id, team_id)
) ENGINE=InnoDB;
";
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();

            try
            {
                using var alterCmd = new MySqlCommand("ALTER TABLE battle_rounds ADD COLUMN final_bar_state JSON NULL AFTER winning_team_id;", connection);
                await alterCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Column may already exist
            }
        }
        catch
        {
            // Ignore if test db is unavailable
        }
    }

    private static async Task CleanupTestDataAsync(int matchId)
    {
        try
        {
            using var connection = new MySqlConnection(TestConnectionString);
            await connection.OpenAsync();

            using var cmdRounds = new MySqlCommand("DELETE FROM battle_rounds WHERE match_id = @MatchId;", connection);
            cmdRounds.Parameters.AddWithValue("@MatchId", matchId);
            await cmdRounds.ExecuteNonQueryAsync();

            using var cmdBars = new MySqlCommand("DELETE FROM battle_bars WHERE match_id = @MatchId;", connection);
            cmdBars.Parameters.AddWithValue("@MatchId", matchId);
            await cmdBars.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
