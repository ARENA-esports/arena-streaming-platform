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
}
