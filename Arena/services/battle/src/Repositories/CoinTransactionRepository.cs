using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public class CoinTransactionRepository : ICoinTransactionRepository
{
    private readonly string _connectionString;

    public CoinTransactionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<long> RecordTransactionAsync(int userId, int walletId, int amount, string type, int? streamId)
    {
        using var connection = CreateConnection();
        const string sql = @"
            INSERT INTO coin_transactions (user_id, wallet_id, amount, transaction_type, stream_id)
            VALUES (@UserId, @WalletId, @Amount, @Type, @StreamId);
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            UserId = userId,
            WalletId = walletId,
            Amount = amount,
            Type = type,
            StreamId = streamId
        });
    }

    public async Task<IEnumerable<CoinTransaction>> GetRecentTransactionsAsync(int userId, int limit = 50)
    {
        using var connection = CreateConnection();
        const string sql = @"
            SELECT * FROM coin_transactions
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT @Limit;";

        return await connection.QueryAsync<CoinTransaction>(sql, new { UserId = userId, Limit = limit });
    }

    /// <summary>
    /// Returns the sum of WATCH_TICK coins earned by the specified user for the specified stream
    /// within the rolling window [windowStart, now]. Uses the composite index
    /// idx_transactions_user_stream_created(user_id, stream_id, created_at) for efficient range access.
    /// When streamId is null, returns 0 immediately (no global-per-user aggregation is applied).
    /// </summary>
    public async Task<int> GetWindowCoinSumAsync(int userId, int? streamId, DateTime windowStart)
    {
        // Per SCRUM-115 design decision: when no stream context is provided,
        // the cap policy does not aggregate across unrelated null-stream requests.
        if (streamId is null)
        {
            return 0;
        }

        using var connection = CreateConnection();

        // The WHERE clause is crafted so the query engine uses the leading columns of
        // idx_transactions_user_stream_created (user_id, stream_id, created_at):
        //   1. user_id  = @UserId     -> equality on first key column
        //   2. stream_id = @StreamId  -> equality on second key column
        //   3. created_at >= @WindowStart -> range on third key column
        // transaction_type filter is applied as a residual predicate inside the index range.
        const string sql = @"
            SELECT COALESCE(SUM(amount), 0)
            FROM coin_transactions
            WHERE user_id        = @UserId
              AND stream_id      = @StreamId
              AND transaction_type = 'WATCH_TICK'
              AND created_at    >= @WindowStart;";

        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            UserId = userId,
            StreamId = streamId.Value,
            WindowStart = windowStart
        });
    }
}
