using MySQLConnector;   // mysql connector for open connection, run queries
using StreamService.DTOs;   // import DTOs
using StreamService.Models; // import domain models 

namespace StreamService.Repositories;

// implementing IMatchRepository to extract and store database connection strings
public class MatchRepositories : IMatchRepository{
    private readonly string _connectionString;  // store connection string in an immutable field for use in every ADO.NET method

    public MatchRepository(IConfiguration configuration){   // constructor run when MatchRepository is created. expect IConfiguration (ASP.NET Core configuration)
        // look up connection string name is "DefaultConnection" in appsettings.json or environment variables
        _connectionString=configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    public async Task<bool> BothTeamsExistAsync(int TeamAId, int TeamBId){
        using var connection = new MySQLConnection(_connectionString);   // instantiate concrete mysql ADO.NET socket connection object
        await connection.OpenAsync();   // send handshake to mysql and open async connection

        /*
        sql verification query with parameters.
        count unique team IDs user submitted. avoid request from break if user passes same IDs twice.
        */
        const string sql = @"
        SELECT COUNT(DISTINCT team_id)
        from teams
        WHERE team_id IN (@TeamA, @TeamB);";

        // create sql command and bind value to prevent sql injection
        using var command = new MySqlCommand(sql, connection);   // create ADO.NET command object and connect it to open MySQL connection
        command.Parameters.AddWithValue("@TeamA", teamAId); // map int variable teamAId directly to @TeamA
        command.Parameters.AddWithValue("@TeamB", teamBId);

        var result = await command.ExecuteScalarAsync();    // execute scalar query asynchronously and store return value as object?
        /*
            convert sql scalar output into 32-bit integer.
            if query return null or DBNull.Value -> convert it into 0
        */
        var count = Convert.ToInt32(result);
        return count == 2;  // return true only if both teams exist in DB
    }

    public async Task<int> CreateMatchAsync(int TournamentId, int teamAId, int TeamBId, DateTimeOffset ScheduledTime){
        using var connection = new MySQLConnection(_connectionString);
        await connection.OpenAsync();   // asynchronously open TCP connection

        /*
            sql query to insert matches and immediately retrieve auto-generated primary key
        */
        const string sql = @"
        INSERT INTO matches (tournament_id, team_a_id, team_b_id, scheduled_time, status) 
        VALUES (@TournamentId, @TeamA, @TeamB, @ScheduledTime, @Status);
        SELECT LAST_INSERT_ID();
        "
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TournamentId", tournament_id);
        command.Parameters.AddWithValue("@TeamA", teamAId);
        command.Parameters.AddWithValue("@TeamB", teamBId);
        command.Parameters.AddWithValue("@ScheduledTime", scheduledTime.UtcDateTime);   // converts the DateTimeOffset to a UTC DateTime
        command.Parameters.AddWithValue("@Status", StreamService.Scheduled);    // use the constant "Scheduled" from Models/StreamStatus.cs, ensure enum

        result = await command.ExecuteScalarAsync();    // insert match and return entry primary key
        return Convert.ToInt32(result);  // convert UInt64 match_id returned from sql into 32-bit int
    }
}