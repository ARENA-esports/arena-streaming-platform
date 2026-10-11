using MySqlConnector;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

/// <summary>
/// MySQL implementation of <see cref="IBattleRoundRepository"/> (SCRUM-122).
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
            SELECT round_id, match_id, round_number, target_damage, winning_team_id, round_active, created_at, ended_at
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
            // 1. Reset battle bar damage totals for this match
            const string resetBarsSql = @"
                UPDATE battle_bars
                SET total_damage = 0
                WHERE match_id = @MatchId;";

            using var resetCmd = new MySqlCommand(resetBarsSql, connection, transaction);
            resetCmd.Parameters.AddWithValue("@MatchId", matchId);
            await resetCmd.ExecuteNonQueryAsync();

            // 2. Insert and activate next round
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

    private static BattleRound MapReaderToBattleRound(MySqlDataReader reader)
    {
        var winningTeamOrdinal = reader.GetOrdinal("winning_team_id");
        var endedAtOrdinal = reader.GetOrdinal("ended_at");

        return new BattleRound
        {
            RoundId = reader.GetInt32(reader.GetOrdinal("round_id")),
            MatchId = reader.GetInt32(reader.GetOrdinal("match_id")),
            RoundNumber = reader.GetInt32(reader.GetOrdinal("round_number")),
            TargetDamage = reader.GetInt64(reader.GetOrdinal("target_damage")),
            WinningTeamId = reader.IsDBNull(winningTeamOrdinal) ? null : reader.GetInt32(winningTeamOrdinal),
            RoundActive = reader.GetBoolean(reader.GetOrdinal("round_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            EndedAt = reader.IsDBNull(endedAtOrdinal) ? null : reader.GetDateTime(endedAtOrdinal)
        };
    }
}
