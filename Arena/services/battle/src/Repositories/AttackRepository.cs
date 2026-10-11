using MySqlConnector;
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
}
