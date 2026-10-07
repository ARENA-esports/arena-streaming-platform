using System.Text.Json;
using MySqlConnector;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

/// <summary>
/// MySQL implementation of <see cref="IBattleRoundRepository"/> (SCRUM-122, SCRUM-123).
/// Uses single-statement atomic UPDATE for check-and-flip flag synchronization.
/// </summary>
public class BattleRoundRepository : IBattleRoundRepository
{
    private readonly string _connectionString;

    public BattleRoundRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    /// <inheritdoc />
    public async Task<BattleRound?> GetActiveRoundAsync(int matchId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT round_id, match_id, round_number, target_damage, winning_team_id, round_active, final_bar_state, created_at, ended_at
            FROM battle_rounds
            WHERE match_id = @MatchId AND round_active = TRUE
            ORDER BY round_number DESC
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MatchId", matchId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapReaderToBattleRound(reader);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<BattleRound> GetOrCreateActiveRoundAsync(int matchId, long targetDamage)
    {
        var existing = await GetActiveRoundAsync(matchId);
        if (existing != null)
        {
            return existing;
        }

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Auto-initialize Round 1 if no round exists yet.
        // INSERT IGNORE handles race condition if two concurrent requests attempt to create Round 1.
        const string insertSql = @"
            INSERT IGNORE INTO battle_rounds (match_id, round_number, target_damage, round_active)
            VALUES (@MatchId, 1, @TargetDamage, TRUE);";

        using var insertCmd = new MySqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@MatchId", matchId);
        insertCmd.Parameters.AddWithValue("@TargetDamage", targetDamage);
        await insertCmd.ExecuteNonQueryAsync();

        // Fetch active round after insert
        var activeRound = await GetActiveRoundAsync(matchId);
        if (activeRound != null)
        {
            return activeRound;
        }

        throw new InvalidOperationException($"Failed to retrieve or create active round for match {matchId}.");
    }

    /// <inheritdoc />
    public async Task<bool> TryFlipRoundActiveAsync(int roundId, int winningTeamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Single atomic UPDATE:
        // InnoDB acquires an exclusive row lock on the matching row during UPDATE.
        // If round_active is currently TRUE, it flips to FALSE and returns RowsAffected = 1.
        // Any concurrent threads attempting the same UPDATE will wait for the lock,
        // and subsequently see round_active = FALSE (WHERE condition evaluates to false),
        // resulting in RowsAffected = 0.
        // This guarantees that exactly one process triggers the round reset.
        const string sql = @"
            UPDATE battle_rounds
            SET round_active = FALSE,
                winning_team_id = @WinningTeamId,
                ended_at = CURRENT_TIMESTAMP
            WHERE round_id = @RoundId AND round_active = TRUE;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RoundId", roundId);
        command.Parameters.AddWithValue("@WinningTeamId", winningTeamId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<BattleRound> ResetBarsAndStartNextRoundAsync(int matchId, int nextRoundNumber, long targetDamage)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Snapshot current battle bars before reset (SCRUM-123)
            var currentBars = new List<BattleBarDto>();
            const string selectBarsSql = @"
                SELECT team_id, total_damage
                FROM battle_bars
                WHERE match_id = @MatchId;";

            using (var selectCmd = new MySqlCommand(selectBarsSql, connection, transaction))
            {
                selectCmd.Parameters.AddWithValue("@MatchId", matchId);
                using var reader = await selectCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    currentBars.Add(new BattleBarDto
                    {
                        TeamId = reader.GetInt32(reader.GetOrdinal("team_id")),
                        TotalDamage = reader.GetInt64(reader.GetOrdinal("total_damage"))
                    });
                }
            }

            var finalBarsJson = JsonSerializer.Serialize(currentBars);

            // 2. Persist final_bar_state to the completed round (nextRoundNumber - 1)
            const string updateFinalBarSql = @"
                UPDATE battle_rounds
                SET final_bar_state = @FinalBarState
                WHERE match_id = @MatchId AND round_number = @CompletedRoundNumber;";

            using (var updateCmd = new MySqlCommand(updateFinalBarSql, connection, transaction))
            {
                updateCmd.Parameters.AddWithValue("@FinalBarState", finalBarsJson);
                updateCmd.Parameters.AddWithValue("@MatchId", matchId);
                updateCmd.Parameters.AddWithValue("@CompletedRoundNumber", nextRoundNumber - 1);
                await updateCmd.ExecuteNonQueryAsync();
            }

            // 3. Reset battle bar damage totals for this match
            const string resetBarsSql = @"
                UPDATE battle_bars
                SET total_damage = 0
                WHERE match_id = @MatchId;";

            using var resetCmd = new MySqlCommand(resetBarsSql, connection, transaction);
            resetCmd.Parameters.AddWithValue("@MatchId", matchId);
            await resetCmd.ExecuteNonQueryAsync();

            // 4. Insert and activate next round
            const string nextRoundSql = @"
                INSERT INTO battle_rounds (match_id, round_number, target_damage, round_active)
                VALUES (@MatchId, @NextRoundNumber, @TargetDamage, TRUE);
                SELECT LAST_INSERT_ID();";

            using var nextRoundCmd = new MySqlCommand(nextRoundSql, connection, transaction);
            nextRoundCmd.Parameters.AddWithValue("@MatchId", matchId);
            nextRoundCmd.Parameters.AddWithValue("@NextRoundNumber", nextRoundNumber);
            nextRoundCmd.Parameters.AddWithValue("@TargetDamage", targetDamage);

            var nextRoundId = Convert.ToInt32(await nextRoundCmd.ExecuteScalarAsync());

            await transaction.CommitAsync();

            return new BattleRound
            {
                RoundId = nextRoundId,
                MatchId = matchId,
                RoundNumber = nextRoundNumber,
                TargetDamage = targetDamage,
                RoundActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<BattleRound>> GetRoundHistoryAsync(int matchId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT round_id, match_id, round_number, target_damage, winning_team_id, round_active, final_bar_state, created_at, ended_at
            FROM battle_rounds
            WHERE match_id = @MatchId AND round_active = FALSE
            ORDER BY round_number DESC;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MatchId", matchId);

        var rounds = new List<BattleRound>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rounds.Add(MapReaderToBattleRound(reader));
        }

        return rounds;
    }

    private static BattleRound MapReaderToBattleRound(MySqlDataReader reader)
    {
        var winningTeamOrdinal = reader.GetOrdinal("winning_team_id");
        var endedAtOrdinal = reader.GetOrdinal("ended_at");
        int finalBarStateOrdinal;
        try
        {
            finalBarStateOrdinal = reader.GetOrdinal("final_bar_state");
        }
        catch (IndexOutOfRangeException)
        {
            finalBarStateOrdinal = -1;
        }

        return new BattleRound
        {
            RoundId = reader.GetInt32(reader.GetOrdinal("round_id")),
            MatchId = reader.GetInt32(reader.GetOrdinal("match_id")),
            RoundNumber = reader.GetInt32(reader.GetOrdinal("round_number")),
            TargetDamage = reader.GetInt64(reader.GetOrdinal("target_damage")),
            WinningTeamId = reader.IsDBNull(winningTeamOrdinal) ? null : reader.GetInt32(winningTeamOrdinal),
            FinalBarState = finalBarStateOrdinal >= 0 && !reader.IsDBNull(finalBarStateOrdinal)
                ? reader.GetString(finalBarStateOrdinal)
                : null,
            RoundActive = reader.GetBoolean(reader.GetOrdinal("round_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            EndedAt = reader.IsDBNull(endedAtOrdinal) ? null : reader.GetDateTime(endedAtOrdinal)
        };
    }
}
