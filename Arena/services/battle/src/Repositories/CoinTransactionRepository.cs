using System.Data;
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

    public async Task<long> RecordTransactionAsync(int userId, int walletId, int amount, string type, int? streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = @"
            INSERT INTO coin_transactions (user_id, wallet_id, amount, transaction_type, stream_id)
            VALUES (@UserId, @WalletId, @Amount, @Type, @StreamId);
            SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@WalletId", walletId);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@Type", type);
        command.Parameters.AddWithValue("@StreamId", (object?)streamId ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<IEnumerable<CoinTransaction>> GetRecentTransactionsAsync(int userId, int limit = 50)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = @"
            SELECT transaction_id, user_id, wallet_id, amount, transaction_type, stream_id, created_at
            FROM coin_transactions
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT @Limit;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Limit", limit);

        using var reader = await command.ExecuteReaderAsync();
        var list = new List<CoinTransaction>();
        while (await reader.ReadAsync())
        {
            list.Add(new CoinTransaction
            {
                TransactionId = reader.GetInt64(reader.GetOrdinal("transaction_id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                WalletId = reader.GetInt32(reader.GetOrdinal("wallet_id")),
                Amount = reader.GetInt32(reader.GetOrdinal("amount")),
                TransactionType = reader.GetString(reader.GetOrdinal("transaction_type")),
                StreamId = reader.IsDBNull(reader.GetOrdinal("stream_id")) ? null : reader.GetInt32(reader.GetOrdinal("stream_id")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    public async Task<int> GetWindowCoinSumAsync(int userId, int? streamId, DateTime windowStart)
    {
        if (streamId is null)
        {
            return 0;
        }

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT COALESCE(SUM(amount), 0)
            FROM coin_transactions
            WHERE user_id        = @UserId
              AND stream_id      = @StreamId
              AND transaction_type = 'WATCH_TICK'
              AND created_at    >= @WindowStart;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@StreamId", streamId.Value);
        command.Parameters.AddWithValue("@WindowStart", windowStart);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
