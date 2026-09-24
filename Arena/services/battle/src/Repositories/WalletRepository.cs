using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly string _connectionString;

    public WalletRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<Wallet?> GetByUserIdAsync(int userId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM wallets WHERE user_id = @UserId LIMIT 1;";
        return await connection.QueryFirstOrDefaultAsync<Wallet>(sql, new { UserId = userId });
    }

    public async Task<Wallet> GetOrCreateWalletAsync(int userId)
    {
        using var connection = CreateConnection();

        // Ensure wallet exists with initial coins = 0 and last_tick_at = NULL
        const string insertSql = @"
            INSERT INTO wallets (user_id, coins, last_tick_at)
            VALUES (@UserId, 0, NULL)
            ON DUPLICATE KEY UPDATE user_id = user_id;";
        await connection.ExecuteAsync(insertSql, new { UserId = userId });

        const string selectSql = "SELECT * FROM wallets WHERE user_id = @UserId LIMIT 1;";
        var wallet = await connection.QueryFirstOrDefaultAsync<Wallet>(selectSql, new { UserId = userId });
        return wallet ?? throw new InvalidOperationException($"Failed to retrieve or create wallet for user {userId}.");
    }

    /// <summary>
    /// Executes an atomic conditional update enforcing the minimum-interval anti-farm rule (AC3).
    /// If last_tick_at is NULL (first award) or last_tick_at &lt;= threshold, the update succeeds.
    /// If another concurrent request beats this one or the interval is not met, 0 rows are affected.
    /// </summary>
    public async Task<bool> TryAwardWatchTickAsync(int userId, int coins, DateTime currentTime, DateTime threshold)
    {
        using var connection = CreateConnection();

        const string sql = @"
            UPDATE wallets
            SET coins = coins + @Coins,
                last_tick_at = @CurrentTime
            WHERE user_id = @UserId
              AND (last_tick_at IS NULL OR last_tick_at <= @Threshold);";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Coins = coins,
            CurrentTime = currentTime,
            UserId = userId,
            Threshold = threshold
        });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Atomically executes the conditional wallet award AND inserts the coin_transaction ledger record
    /// within a single ACID database transaction.
    /// If the conditional update rejects (0 rows affected), transaction is rolled back and no ledger record is created.
    /// </summary>
    public async Task<AwardResult> ExecuteWatchTickAwardAsync(int userId, int coins, DateTime currentTime, DateTime threshold, int? streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Atomic conditional update (AC3)
            const string updateSql = @"
                UPDATE wallets
                SET coins = coins + @Coins,
                    last_tick_at = @CurrentTime
                WHERE user_id = @UserId
                  AND (last_tick_at IS NULL OR last_tick_at <= @Threshold);";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new
            {
                Coins = coins,
                CurrentTime = currentTime,
                UserId = userId,
                Threshold = threshold
            }, transaction);

            if (rowsAffected == 0)
            {
                // Rate-limited or concurrent collision: query current state without altering
                const string querySql = "SELECT * FROM wallets WHERE user_id = @UserId LIMIT 1;";
                var existing = await connection.QueryFirstOrDefaultAsync<Wallet>(querySql, new { UserId = userId }, transaction);
                await transaction.RollbackAsync();

                return AwardResult.RateLimited(existing?.Coins ?? 0, existing?.LastTickAt);
            }

            // 2. Fetch updated balance
            const string selectSql = "SELECT * FROM wallets WHERE user_id = @UserId LIMIT 1;";
            var updatedWallet = await connection.QueryFirstOrDefaultAsync<Wallet>(selectSql, new { UserId = userId }, transaction);

            if (updatedWallet == null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Wallet for user {userId} could not be loaded after update.");
            }

            // 3. Atomically insert coin transaction ledger entry
            const string insertTxSql = @"
                INSERT INTO coin_transactions (user_id, wallet_id, amount, transaction_type, stream_id)
                VALUES (@UserId, @WalletId, @Amount, @Type, @StreamId);";

            await connection.ExecuteAsync(insertTxSql, new
            {
                UserId = userId,
                WalletId = updatedWallet.WalletId,
                Amount = coins,
                Type = "WATCH_TICK",
                StreamId = streamId
            }, transaction);

            await transaction.CommitAsync();

            return AwardResult.Succeeded(coins, updatedWallet.Coins, currentTime, updatedWallet.WalletId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
