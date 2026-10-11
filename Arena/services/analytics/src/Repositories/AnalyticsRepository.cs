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
