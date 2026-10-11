using MySqlConnector;
using BattleEconomyService.Models;

namespace BattleEconomyService.Repositories;

public class WeaponRepository : IWeaponRepository
{
    private readonly string _connectionString;

    public WeaponRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    public async Task<List<Weapon>> GetActiveWeaponsAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT weapon_id, name, description, cost, damage, icon_key, is_active, created_at
            FROM weapons
            WHERE is_active = TRUE
            ORDER BY cost ASC;";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var weapons = new List<Weapon>();
        while (await reader.ReadAsync())
        {
            weapons.Add(MapWeapon(reader));
        }
        return weapons;
    }

    public async Task<Weapon?> GetByIdAsync(int weaponId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT weapon_id, name, description, cost, damage, icon_key, is_active, created_at
            FROM weapons
            WHERE weapon_id = @WeaponId
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@WeaponId", weaponId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapWeapon(reader);
        }
        return null;
    }

    private static Weapon MapWeapon(MySqlDataReader reader)
    {
        return new Weapon
        {
            WeaponId = reader.GetInt32(reader.GetOrdinal("weapon_id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.GetString(reader.GetOrdinal("description")),
            Cost = reader.GetInt32(reader.GetOrdinal("cost")),
            Damage = reader.GetInt32(reader.GetOrdinal("damage")),
            IconKey = reader.GetString(reader.GetOrdinal("icon_key")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}
