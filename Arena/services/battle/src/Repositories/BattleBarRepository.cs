using MySqlConnector;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

/// <summary>
/// MySQL implementation of <see cref="IBattleBarRepository"/> (SCRUM-120).
/// Uses INSERT ... ON DUPLICATE KEY UPDATE for lock-free atomic damage increments.
/// </summary>
public class BattleBarRepository : IBattleBarRepository
{
    private readonly string _connectionString;

    public BattleBarRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    /// <inheritdoc />
    public async Task<long> ApplyDamageAtomicAsync(int matchId, int teamId, int damage)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Atomic upsert: creates the bar on first attack, increments on subsequent attacks.
        // InnoDB acquires a row-level exclusive lock during UPDATE, serialising concurrent
        // writes to the same (match_id, team_id) and preventing lost updates.
        const string sql = @"
            INSERT INTO battle_bars (match_id, team_id, total_damage)
            VALUES (@MatchId, @TeamId, @Damage)
            ON DUPLICATE KEY UPDATE total_damage = total_damage + @Damage;

            SELECT total_damage FROM battle_bars
            WHERE match_id = @MatchId AND team_id = @TeamId;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MatchId", matchId);
        command.Parameters.AddWithValue("@TeamId", teamId);
        command.Parameters.AddWithValue("@Damage", damage);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    /// <inheritdoc />
    public async Task<List<BattleBar>> GetBarsForMatchAsync(int matchId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT bar_id, match_id, team_id, total_damage, created_at, updated_at
            FROM battle_bars
            WHERE match_id = @MatchId
            ORDER BY team_id;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MatchId", matchId);

        var bars = new List<BattleBar>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            bars.Add(new BattleBar
            {
                BarId = reader.GetInt32(reader.GetOrdinal("bar_id")),
                MatchId = reader.GetInt32(reader.GetOrdinal("match_id")),
                TeamId = reader.GetInt32(reader.GetOrdinal("team_id")),
                TotalDamage = reader.GetInt64(reader.GetOrdinal("total_damage")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            });
        }

        return bars;
    }
}
