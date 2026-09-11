using Microsoft.Extensions.Configuration;
using MySqlConnector;
using TournamentService.DTOs;
using TournamentService.Models;

namespace TournamentService.Repositories;

public class TournamentRepository : ITournamentRepository
{
    private readonly string _connectionString;

    public TournamentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    public async Task<int> CreateTournamentAsync(string name, string seasonIdentifier, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO tournaments (name, season_identifier, start_date, end_date, status)
            VALUES (@Name, @SeasonIdentifier, @StartDate, @EndDate, @Status);
            SELECT LAST_INSERT_ID();";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@SeasonIdentifier", seasonIdentifier);
        command.Parameters.AddWithValue("@StartDate", startDate.UtcDateTime);
        command.Parameters.AddWithValue("@EndDate", endDate.UtcDateTime);
        command.Parameters.AddWithValue("@Status", TournamentStatus.Scheduled);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<TournamentResponse?> GetTournamentByIdAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT id, name, season_identifier, start_date, end_date, status, created_at, updated_at
            FROM tournaments
            WHERE id = @Id
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapTournamentResponse(reader);
        }

        return null;
    }

    public async Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync()
    {
        var tournaments = new List<TournamentResponse>();
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT id, name, season_identifier, start_date, end_date, status, created_at, updated_at
            FROM tournaments
            ORDER BY start_date ASC;";

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tournaments.Add(MapTournamentResponse(reader));
        }

        return tournaments;
    }

    public async Task<bool> UpdateTournamentAsync(int id, string name, string seasonIdentifier, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE tournaments
            SET name = @Name,
                season_identifier = @SeasonIdentifier,
                start_date = @StartDate,
                end_date = @EndDate
            WHERE id = @Id
              AND status NOT IN ('Cancelled', 'Completed');";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@SeasonIdentifier", seasonIdentifier);
        command.Parameters.AddWithValue("@StartDate", startDate.UtcDateTime);
        command.Parameters.AddWithValue("@EndDate", endDate.UtcDateTime);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> CancelTournamentAsync(int id)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Atomic conditional update ensuring only non-completed and non-cancelled tournaments transition to Cancelled
        const string sql = @"
            UPDATE tournaments
            SET status = @NewStatus
            WHERE id = @Id
              AND status NOT IN ('Cancelled', 'Completed');";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NewStatus", TournamentStatus.Cancelled);
        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private static TournamentResponse MapTournamentResponse(MySqlDataReader reader)
    {
        return new TournamentResponse(
            reader.GetInt32("id"),
            reader.GetString("name"),
            reader.GetString("season_identifier"),
            new DateTimeOffset(reader.GetDateTime("start_date"), TimeSpan.Zero),
            new DateTimeOffset(reader.GetDateTime("end_date"), TimeSpan.Zero),
            reader.GetString("status"),
            reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime("created_at"),
            reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime("updated_at")
        );
    }
}
