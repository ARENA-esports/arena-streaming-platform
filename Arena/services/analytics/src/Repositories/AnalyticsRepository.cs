using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using AnalyticsService.Entities;

namespace AnalyticsService.Repositories;

/// <summary>
/// Raw ADO.NET implementation of <see cref="IAnalyticsRepository"/> using MySqlConnector.
/// Strictly avoids ORMs, ensuring all SQL commands are explicit, parameterized, and transaction-safe.
/// </summary>
public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly string _connectionString;

    public AnalyticsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    /// <inheritdoc />
    public async Task<bool> RecordCoinEarnedAsync(
        int streamId,
        int userId,
        int amount,
        DateTime timestampUtc,
        int watchDurationSeconds)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Idempotency guard: insert into stream_engagement_events.
            // Duplicate (user_id, stream_id, event_timestamp) delivery will result in 0 rows affected.
            const string insertEventSql = @"
                INSERT IGNORE INTO stream_engagement_events 
                    (user_id, stream_id, event_timestamp, amount, created_at)
                VALUES 
                    (@UserId, @StreamId, @TimestampUtc, @Amount, UTC_TIMESTAMP());";

            using (var eventCmd = new MySqlCommand(insertEventSql, connection, transaction))
            {
                eventCmd.Parameters.AddWithValue("@UserId", userId);
                eventCmd.Parameters.AddWithValue("@StreamId", streamId);
                eventCmd.Parameters.AddWithValue("@TimestampUtc", timestampUtc);
                eventCmd.Parameters.AddWithValue("@Amount", amount);

                var rowsAffected = await eventCmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    // Message was already processed; safely acknowledge and do not double-aggregate.
                    await transaction.RollbackAsync();
                    return false;
                }
            }

            // 2. Check if this is the first recorded watch-tick event for this viewer on this stream.
            const string countPriorEventsSql = @"
                SELECT COUNT(1) FROM stream_engagement_events
                WHERE stream_id = @StreamId 
                  AND user_id = @UserId 
                  AND event_timestamp < @TimestampUtc;";

            bool isNewViewer;
            using (var countCmd = new MySqlCommand(countPriorEventsSql, connection, transaction))
            {
                countCmd.Parameters.AddWithValue("@StreamId", streamId);
                countCmd.Parameters.AddWithValue("@UserId", userId);
                countCmd.Parameters.AddWithValue("@TimestampUtc", timestampUtc);

                var priorCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
                isNewViewer = (priorCount == 0);
            }

            // 3. Upsert aggregate read model in stream_engagement_summary.
            const string upsertSummarySql = @"
                INSERT INTO stream_engagement_summary 
                    (stream_id, total_watch_seconds, total_coins_earned, total_watch_ticks, unique_viewers, last_event_at)
                VALUES 
                    (@StreamId, @WatchSeconds, @Amount, 1, 1, @TimestampUtc)
                ON DUPLICATE KEY UPDATE
                    total_watch_seconds = total_watch_seconds + VALUES(total_watch_seconds),
                    total_coins_earned = total_coins_earned + VALUES(total_coins_earned),
                    total_watch_ticks = total_watch_ticks + 1,
                    unique_viewers = unique_viewers + IF(@IsNewViewer = 1, 1, 0),
                    last_event_at = GREATEST(COALESCE(last_event_at, VALUES(last_event_at)), VALUES(last_event_at));";

            using (var summaryCmd = new MySqlCommand(upsertSummarySql, connection, transaction))
            {
                summaryCmd.Parameters.AddWithValue("@StreamId", streamId);
                summaryCmd.Parameters.AddWithValue("@WatchSeconds", watchDurationSeconds);
                summaryCmd.Parameters.AddWithValue("@Amount", amount);
                summaryCmd.Parameters.AddWithValue("@TimestampUtc", timestampUtc);
                summaryCmd.Parameters.AddWithValue("@IsNewViewer", isNewViewer ? 1 : 0);

                await summaryCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<StreamEngagementSummary?> GetEngagementSummaryByStreamIdAsync(int streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, total_watch_seconds, total_coins_earned, total_watch_ticks, 
                   unique_viewers, last_event_at, created_at, updated_at
            FROM stream_engagement_summary
            WHERE stream_id = @StreamId
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", streamId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapStreamEngagementSummary(reader);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamEngagementSummary>> GetAllEngagementSummariesAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, total_watch_seconds, total_coins_earned, total_watch_ticks, 
                   unique_viewers, last_event_at, created_at, updated_at
            FROM stream_engagement_summary
            ORDER BY total_watch_seconds DESC, total_coins_earned DESC;";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var list = new List<StreamEngagementSummary>();
        while (await reader.ReadAsync())
        {
            list.Add(MapStreamEngagementSummary(reader));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamTeamBattleSummary>> GetBattleSummariesByStreamIdAsync(int streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, team_id, team_name, total_attacks, total_damage_dealt, 
                   total_coins_spent, rounds_won, rounds_lost, last_attack_at, created_at, updated_at
            FROM stream_team_battle_summary
            WHERE stream_id = @StreamId
            ORDER BY total_attacks DESC, total_damage_dealt DESC;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", streamId);

        using var reader = await command.ExecuteReaderAsync();
        var list = new List<StreamTeamBattleSummary>();
        while (await reader.ReadAsync())
        {
            list.Add(MapStreamTeamBattleSummary(reader));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamTeamBattleSummary>> GetAllBattleSummariesAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, team_id, team_name, total_attacks, total_damage_dealt, 
                   total_coins_spent, rounds_won, rounds_lost, last_attack_at, created_at, updated_at
            FROM stream_team_battle_summary
            ORDER BY stream_id ASC, total_attacks DESC;";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var list = new List<StreamTeamBattleSummary>();
        while (await reader.ReadAsync())
        {
            list.Add(MapStreamTeamBattleSummary(reader));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamRoundOutcome>> GetRoundOutcomesByStreamIdAsync(int streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, round_number, winning_team_id, winning_team_name, team_a_id, team_b_id,
                   team_a_attacks, team_b_attacks, team_a_damage, team_b_damage, completed_at, created_at
            FROM stream_round_outcomes
            WHERE stream_id = @StreamId
            ORDER BY round_number ASC;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", streamId);

        using var reader = await command.ExecuteReaderAsync();
        var list = new List<StreamRoundOutcome>();
        while (await reader.ReadAsync())
        {
            list.Add(MapStreamRoundOutcome(reader));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamRoundOutcome>> GetAllRoundOutcomesAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, round_number, winning_team_id, winning_team_name, team_a_id, team_b_id,
                   team_a_attacks, team_b_attacks, team_a_damage, team_b_damage, completed_at, created_at
            FROM stream_round_outcomes
            ORDER BY stream_id ASC, round_number ASC;";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var list = new List<StreamRoundOutcome>();
        while (await reader.ReadAsync())
        {
            list.Add(MapStreamRoundOutcome(reader));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task UpsertBattleSummaryAsync(StreamTeamBattleSummary summary)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO stream_team_battle_summary
                (stream_id, team_id, team_name, total_attacks, total_damage_dealt, 
                 total_coins_spent, rounds_won, rounds_lost, last_attack_at)
            VALUES
                (@StreamId, @TeamId, @TeamName, @TotalAttacks, @TotalDamageDealt,
                 @TotalCoinsSpent, @RoundsWon, @RoundsLost, @LastAttackAt)
            ON DUPLICATE KEY UPDATE
                team_name = VALUES(team_name),
                total_attacks = VALUES(total_attacks),
                total_damage_dealt = VALUES(total_damage_dealt),
                total_coins_spent = VALUES(total_coins_spent),
                rounds_won = VALUES(rounds_won),
                rounds_lost = VALUES(rounds_lost),
                last_attack_at = VALUES(last_attack_at);";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", summary.StreamId);
        command.Parameters.AddWithValue("@TeamId", summary.TeamId);
        command.Parameters.AddWithValue("@TeamName", summary.TeamName);
        command.Parameters.AddWithValue("@TotalAttacks", summary.TotalAttacks);
        command.Parameters.AddWithValue("@TotalDamageDealt", summary.TotalDamageDealt);
        command.Parameters.AddWithValue("@TotalCoinsSpent", summary.TotalCoinsSpent);
        command.Parameters.AddWithValue("@RoundsWon", summary.RoundsWon);
        command.Parameters.AddWithValue("@RoundsLost", summary.RoundsLost);
        command.Parameters.AddWithValue("@LastAttackAt", (object?)summary.LastAttackAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task RecordRoundOutcomeAsync(StreamRoundOutcome outcome)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO stream_round_outcomes
                (stream_id, round_number, winning_team_id, winning_team_name, team_a_id, team_b_id,
                 team_a_attacks, team_b_attacks, team_a_damage, team_b_damage, completed_at)
            VALUES
                (@StreamId, @RoundNumber, @WinningTeamId, @WinningTeamName, @TeamAId, @TeamBId,
                 @TeamAAttacks, @TeamBAttacks, @TeamADamage, @TeamBDamage, @CompletedAt)
            ON DUPLICATE KEY UPDATE
                winning_team_id = VALUES(winning_team_id),
                winning_team_name = VALUES(winning_team_name),
                team_a_id = VALUES(team_a_id),
                team_b_id = VALUES(team_b_id),
                team_a_attacks = VALUES(team_a_attacks),
                team_b_attacks = VALUES(team_b_attacks),
                team_a_damage = VALUES(team_a_damage),
                team_b_damage = VALUES(team_b_damage),
                completed_at = VALUES(completed_at);";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", outcome.StreamId);
        command.Parameters.AddWithValue("@RoundNumber", outcome.RoundNumber);
        command.Parameters.AddWithValue("@WinningTeamId", outcome.WinningTeamId);
        command.Parameters.AddWithValue("@WinningTeamName", outcome.WinningTeamName);
        command.Parameters.AddWithValue("@TeamAId", outcome.TeamAId);
        command.Parameters.AddWithValue("@TeamBId", outcome.TeamBId);
        command.Parameters.AddWithValue("@TeamAAttacks", outcome.TeamAAttacks);
        command.Parameters.AddWithValue("@TeamBAttacks", outcome.TeamBAttacks);
        command.Parameters.AddWithValue("@TeamADamage", outcome.TeamADamage);
        command.Parameters.AddWithValue("@TeamBDamage", outcome.TeamBDamage);
        command.Parameters.AddWithValue("@CompletedAt", outcome.CompletedAt);

        await command.ExecuteNonQueryAsync();
    }

    private static StreamTeamBattleSummary MapStreamTeamBattleSummary(IDataRecord record)
    {
        return new StreamTeamBattleSummary
        {
            StreamId = record.GetInt32(record.GetOrdinal("stream_id")),
            TeamId = record.GetInt32(record.GetOrdinal("team_id")),
            TeamName = record.GetString(record.GetOrdinal("team_name")),
            TotalAttacks = record.GetInt32(record.GetOrdinal("total_attacks")),
            TotalDamageDealt = record.GetInt64(record.GetOrdinal("total_damage_dealt")),
            TotalCoinsSpent = record.GetInt64(record.GetOrdinal("total_coins_spent")),
            RoundsWon = record.GetInt32(record.GetOrdinal("rounds_won")),
            RoundsLost = record.GetInt32(record.GetOrdinal("rounds_lost")),
            LastAttackAt = record.IsDBNull(record.GetOrdinal("last_attack_at"))
                ? null
                : record.GetDateTime(record.GetOrdinal("last_attack_at")),
            CreatedAt = record.GetDateTime(record.GetOrdinal("created_at")),
            UpdatedAt = record.GetDateTime(record.GetOrdinal("updated_at"))
        };
    }

    private static StreamRoundOutcome MapStreamRoundOutcome(IDataRecord record)
    {
        return new StreamRoundOutcome
        {
            StreamId = record.GetInt32(record.GetOrdinal("stream_id")),
            RoundNumber = record.GetInt32(record.GetOrdinal("round_number")),
            WinningTeamId = record.GetInt32(record.GetOrdinal("winning_team_id")),
            WinningTeamName = record.GetString(record.GetOrdinal("winning_team_name")),
            TeamAId = record.GetInt32(record.GetOrdinal("team_a_id")),
            TeamBId = record.GetInt32(record.GetOrdinal("team_b_id")),
            TeamAAttacks = record.GetInt32(record.GetOrdinal("team_a_attacks")),
            TeamBAttacks = record.GetInt32(record.GetOrdinal("team_b_attacks")),
            TeamADamage = record.GetInt32(record.GetOrdinal("team_a_damage")),
            TeamBDamage = record.GetInt32(record.GetOrdinal("team_b_damage")),
            CompletedAt = record.GetDateTime(record.GetOrdinal("completed_at")),
            CreatedAt = record.GetDateTime(record.GetOrdinal("created_at"))
        };
    }

    private static StreamEngagementSummary MapStreamEngagementSummary(IDataRecord record)
    {
        return new StreamEngagementSummary
        {
            StreamId = record.GetInt32(record.GetOrdinal("stream_id")),
            TotalWatchSeconds = record.GetInt64(record.GetOrdinal("total_watch_seconds")),
            TotalCoinsEarned = record.GetInt64(record.GetOrdinal("total_coins_earned")),
            TotalWatchTicks = record.GetInt32(record.GetOrdinal("total_watch_ticks")),
            UniqueViewers = record.GetInt32(record.GetOrdinal("unique_viewers")),
            LastEventAt = record.IsDBNull(record.GetOrdinal("last_event_at"))
                ? null
                : record.GetDateTime(record.GetOrdinal("last_event_at")),
            CreatedAt = record.GetDateTime(record.GetOrdinal("created_at")),
            UpdatedAt = record.GetDateTime(record.GetOrdinal("updated_at"))
        };
    }
}
