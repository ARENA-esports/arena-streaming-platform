using System.Data;
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

    public async Task<Wallet?> GetByUserIdAsync(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = "SELECT wallet_id, user_id, coins, last_tick_at, created_at, updated_at FROM wallets WHERE user_id = @UserId LIMIT 1;";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapWallet(reader);
        }
        return null;
    }

    public async Task<Wallet> GetOrCreateWalletAsync(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string insertSql = @"
            INSERT INTO wallets (user_id, coins, last_tick_at)
            VALUES (@UserId, 0, NULL)
            ON DUPLICATE KEY UPDATE user_id = user_id;";
        using var insertCmd = new MySqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@UserId", userId);
        await insertCmd.ExecuteNonQueryAsync();

        const string selectSql = "SELECT wallet_id, user_id, coins, last_tick_at, created_at, updated_at FROM wallets WHERE user_id = @UserId LIMIT 1;";
        using var selectCmd = new MySqlCommand(selectSql, connection);
        selectCmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapWallet(reader);
        }
        throw new InvalidOperationException($"Failed to retrieve or create wallet for user {userId}.");
    }

    public async Task<bool> TryAwardWatchTickAsync(int userId, int coins, DateTime currentTime, DateTime threshold)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE wallets
            SET coins = coins + @Coins,
                last_tick_at = @CurrentTime
            WHERE user_id = @UserId
              AND (last_tick_at IS NULL OR last_tick_at <= @Threshold);";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Coins", coins);
        command.Parameters.AddWithValue("@CurrentTime", currentTime);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Threshold", threshold);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<AwardResult> ExecuteWatchTickAwardAsync(int userId, int coins, DateTime currentTime, DateTime threshold, int? streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string updateSql = @"
                UPDATE wallets
                SET coins = coins + @Coins,
                    last_tick_at = @CurrentTime
                WHERE user_id = @UserId
                  AND (last_tick_at IS NULL OR last_tick_at <= @Threshold);";

            using var updateCmd = new MySqlCommand(updateSql, connection, transaction);
            updateCmd.Parameters.AddWithValue("@Coins", coins);
            updateCmd.Parameters.AddWithValue("@CurrentTime", currentTime);
            updateCmd.Parameters.AddWithValue("@UserId", userId);
            updateCmd.Parameters.AddWithValue("@Threshold", threshold);

            var rowsAffected = await updateCmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                const string querySql = "SELECT wallet_id, user_id, coins, last_tick_at, created_at, updated_at FROM wallets WHERE user_id = @UserId LIMIT 1;";
                using var queryCmd = new MySqlCommand(querySql, connection, transaction);
                queryCmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = await queryCmd.ExecuteReaderAsync();
                Wallet? existing = null;
                if (await reader.ReadAsync())
                {
                    existing = MapWallet(reader);
                }
                await reader.CloseAsync();
                await transaction.RollbackAsync();

                return AwardResult.RateLimited(existing?.Coins ?? 0, existing?.LastTickAt);
            }

            const string selectSql = "SELECT wallet_id, user_id, coins, last_tick_at, created_at, updated_at FROM wallets WHERE user_id = @UserId LIMIT 1;";
            using var selectCmd = new MySqlCommand(selectSql, connection, transaction);
            selectCmd.Parameters.AddWithValue("@UserId", userId);
            using var updatedReader = await selectCmd.ExecuteReaderAsync();
            Wallet? updatedWallet = null;
            if (await updatedReader.ReadAsync())
            {
                updatedWallet = MapWallet(updatedReader);
            }
            await updatedReader.CloseAsync();

            if (updatedWallet == null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Wallet for user {userId} could not be loaded after update.");
            }

            const string insertTxSql = @"
                INSERT INTO coin_transactions (user_id, wallet_id, amount, transaction_type, stream_id)
                VALUES (@UserId, @WalletId, @Amount, @Type, @StreamId);";

            using var insertTxCmd = new MySqlCommand(insertTxSql, connection, transaction);
            insertTxCmd.Parameters.AddWithValue("@UserId", userId);
            insertTxCmd.Parameters.AddWithValue("@WalletId", updatedWallet.WalletId);
            insertTxCmd.Parameters.AddWithValue("@Amount", coins);
            insertTxCmd.Parameters.AddWithValue("@Type", "WATCH_TICK");
            insertTxCmd.Parameters.AddWithValue("@StreamId", (object?)streamId ?? DBNull.Value);

            await insertTxCmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return AwardResult.Succeeded(coins, updatedWallet.Coins, currentTime, updatedWallet.WalletId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static Wallet MapWallet(MySqlDataReader reader)
    {
        return new Wallet
        {
            WalletId = reader.GetInt32(reader.GetOrdinal("wallet_id")),
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            Coins = reader.GetInt32(reader.GetOrdinal("coins")),
            LastTickAt = reader.IsDBNull(reader.GetOrdinal("last_tick_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_tick_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
