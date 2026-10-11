using Microsoft.Extensions.Configuration;
using MySqlConnector;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public class AttackRepository : IAttackRepository
{
    private readonly string _connectionString;

    public AttackRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    public async Task<long> RecordAttackAsync(AttackLog attackLog)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO attack_log (user_id, weapon_id, match_id, team_id, coins_spent, damage_dealt)
            VALUES (@UserId, @WeaponId, @MatchId, @TeamId, @CoinsSpent, @DamageDealt);
            SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", attackLog.UserId);
        command.Parameters.AddWithValue("@WeaponId", attackLog.WeaponId);
        command.Parameters.AddWithValue("@MatchId", attackLog.MatchId);
        command.Parameters.AddWithValue("@TeamId", attackLog.TeamId);
        command.Parameters.AddWithValue("@CoinsSpent", attackLog.CoinsSpent);
        command.Parameters.AddWithValue("@DamageDealt", attackLog.DamageDealt);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<AtomicAttackResult> ExecuteAttackTransactionAsync(AttackLog attackLog, int weaponCost)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Deduct coins atomically with balance check
            const string deductSql = @"
                UPDATE wallets
                SET coins = coins - @Cost
                WHERE user_id = @UserId
                  AND coins >= @Cost;";

            using var deductCmd = new MySqlCommand(deductSql, connection, transaction);
            deductCmd.Parameters.AddWithValue("@Cost", weaponCost);
            deductCmd.Parameters.AddWithValue("@UserId", attackLog.UserId);

            var rowsAffected = await deductCmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                // Insufficient coins or wallet does not exist — read current balance
                const string balanceSql = "SELECT wallet_id, coins FROM wallets WHERE user_id = @UserId LIMIT 1;";
                using var balanceCmd = new MySqlCommand(balanceSql, connection, transaction);
                balanceCmd.Parameters.AddWithValue("@UserId", attackLog.UserId);
                using var balanceReader = await balanceCmd.ExecuteReaderAsync();

                int currentBalance = 0;
                if (await balanceReader.ReadAsync())
                {
                    currentBalance = balanceReader.GetInt32(balanceReader.GetOrdinal("coins"));
                }
                await balanceReader.CloseAsync();
                await transaction.RollbackAsync();

                return AtomicAttackResult.InsufficientCoins(currentBalance);
            }

            // 2. Query updated wallet info
            const string selectWalletSql = "SELECT wallet_id, coins FROM wallets WHERE user_id = @UserId LIMIT 1;";
            using var selectWalletCmd = new MySqlCommand(selectWalletSql, connection, transaction);
            selectWalletCmd.Parameters.AddWithValue("@UserId", attackLog.UserId);
            using var walletReader = await selectWalletCmd.ExecuteReaderAsync();

            int walletId = 0;
            int remainingBalance = 0;
            if (await walletReader.ReadAsync())
            {
                walletId = walletReader.GetInt32(walletReader.GetOrdinal("wallet_id"));
                remainingBalance = walletReader.GetInt32(walletReader.GetOrdinal("coins"));
            }
            await walletReader.CloseAsync();

            if (walletId == 0)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Wallet for user {attackLog.UserId} could not be loaded after deduction.");
            }

            // 3. Record attack log
            const string attackSql = @"
                INSERT INTO attack_log (user_id, weapon_id, match_id, team_id, coins_spent, damage_dealt)
                VALUES (@UserId, @WeaponId, @MatchId, @TeamId, @CoinsSpent, @DamageDealt);
                SELECT LAST_INSERT_ID();";

            using var attackCmd = new MySqlCommand(attackSql, connection, transaction);
            attackCmd.Parameters.AddWithValue("@UserId", attackLog.UserId);
            attackCmd.Parameters.AddWithValue("@WeaponId", attackLog.WeaponId);
            attackCmd.Parameters.AddWithValue("@MatchId", attackLog.MatchId);
            attackCmd.Parameters.AddWithValue("@TeamId", attackLog.TeamId);
            attackCmd.Parameters.AddWithValue("@CoinsSpent", attackLog.CoinsSpent);
            attackCmd.Parameters.AddWithValue("@DamageDealt", attackLog.DamageDealt);

            var attackIdResult = await attackCmd.ExecuteScalarAsync();
            var attackId = Convert.ToInt64(attackIdResult);

            // 4. Record coin transaction audit entry
            const string coinTxSql = @"
                INSERT INTO coin_transactions (user_id, wallet_id, amount, transaction_type, stream_id)
                VALUES (@UserId, @WalletId, @Amount, @Type, @StreamId);";

            using var coinTxCmd = new MySqlCommand(coinTxSql, connection, transaction);
            coinTxCmd.Parameters.AddWithValue("@UserId", attackLog.UserId);
            coinTxCmd.Parameters.AddWithValue("@WalletId", walletId);
            coinTxCmd.Parameters.AddWithValue("@Amount", -weaponCost);
            coinTxCmd.Parameters.AddWithValue("@Type", "WEAPON_PURCHASE");
            coinTxCmd.Parameters.AddWithValue("@StreamId", DBNull.Value);

            await coinTxCmd.ExecuteNonQueryAsync();

            // 5. Commit transaction
            await transaction.CommitAsync();

            return AtomicAttackResult.Succeeded(attackId, remainingBalance, walletId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
