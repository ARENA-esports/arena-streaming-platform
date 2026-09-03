using Microsoft.Extensions.Configuration;
using MySqlConnector;
using StreamService.DTOs;
using StreamService.Models;

namespace StreamService.Repositories;

public class StreamRepository : IStreamRepository
{
    private readonly string _connectionString;  // store db connection immutably for ADO.NEt queries

    /* Configure Database Connection Injection */
    public StreamRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")      // inject IConfiguration and call default connection
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");// fail fast if connection string in missing from config or env
    }

    /* Fixture and Stream Existence Checks */
    public async Task<bool> MatchExistsAsync(int matchId)
    {
        using var connection = new MySqlConnection(_connectionString);   // instantiates ADO.NET socket
        await connection.OpenAsync();       // open network connection asynchronously

        const string sql = "SELECT COUNT(1) FROM matches WHERE match_id = @MatchId;";   // check for row presence without fetching all rows
        using var command = new MySqlCommand(sql, connection);        // // ensure network sockets and command objects are disposed
        command.Parameters.AddWithValue("@MatchId", matchId);       // parameterize input to block sql injection attacks
        var count = Convert.ToInt32(await command.ExecuteScalarAsync()); //executes the scalar query
        return count >0;        // converts the count into a boolean
    }

    public async Task<bool> StreamExistsForMatchAsync(int matchId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(1) FROM streams WHERE match_id = @MatchId;";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MatchId", matchId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count >0;
    }

    /* Stream Linking Insert Method */
    public async Task<int> LinkStreamToMatchAsync(int matchId, int streamerId, int tournamentId, LinkStreamRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO streams (
                streamer_id, tournament_id, match_id, channel_name, platform, stream_title, embed_parent_domain, status
            ) VALUES (
                @StreamerId, @TournamentId, @MatchId, @ChannelName, @Platform, @StreamTitle, @EmbedParentDomain, @Status
            );
            SELECT LAST_INSERT_ID();";
        
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamerId", streamerId);
        command.Parameters.AddWithValue("@TournamentId", tournamentId);
        command.Parameters.AddWithValue("@MatchId", matchId);
        command.Parameters.AddWithValue("@ChannelName", request.ChannelName.Trim());
        command.Parameters.AddWithValue("@Platform", request.Platform);
        command.Parameters.AddWithValue("@StreamTitle", request.StreamTitle);
        command.Parameters.AddWithValue("@EmbedParentDomain", request.EmbedParentDomain.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@Status", StreamStatus.Scheduled);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /* Helper for Null-Safe Row Mapping */
    private static StreamResponse MapReaderToStreamResponse(MySqlDataReader reader)
    {
        return new StreamResponse(
            reader.GetInt32("stream_id"),
            reader.GetInt32("streamer_id"),
            reader.IsDBNull(reader.GetOrdinal("tournament_id")) ? null : reader.GetInt32("tournament_id"),
            reader.IsDBNull(reader.GetOrdinal("match_id")) ? null : reader.GetInt32("match_id"),
            reader.GetString("channel_name"),
            reader.GetString("platform"),
            reader.GetString("stream_title"),
            reader.IsDBNull(reader.GetOrdinal("embed_parent_domain")) ? null : reader.GetString("embed_parent_domain"),
            reader.GetString("status"),
            reader.GetInt32("viewer_count"),
            reader.IsDBNull(reader.GetOrdinal("started_at")) ? null : reader.GetDateTime("started_at"),
            reader.IsDBNull(reader.GetOrdinal("ended_at")) ? null : reader.GetDateTime("ended_at"),
            reader.GetDateTime("created_at")
        );
    }

    /* Query Methods */
    public async Task<StreamResponse?> GetStreamByIdAsync(int streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, streamer_id, tournament_id, match_id, channel_name, platform,
                    stream_title, embed_parent_domain, status, viewer_count, started_at, ended_at, created_at
            FROM streams
            WHERE stream_id = @StreamId
            LIMIT 1;";

            using var command = new MySqlCommand(sql,connection);
            command.Parameters.AddWithValue("@StreamId", streamId);

            /* execute query and get MySqlDataReader forward only stream rows */
            using var reader =await command.ExecuteReaderAsync();
            if(await reader.ReadAsync())    // move to first row if exist
            {
                return MapReaderToStreamResponse(reader);   // if found map row into stream response
            }
            return null;    // if not found return null
    }

    public async Task<StreamResponse?> GetStreamByMatchIdAsync(int matchId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT stream_id, streamer_id, tournament_id, match_id, channel_name, platform,
                    stream_title, embed_parent_domain, status, viewer_count, started_at, ended_at, created_at
            FROM streams
            WHERE match_id = @MatchId
            LIMIT 1;";
        
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MatchId",matchId);

        /* execute query and get MySqlDataReader forward only stream rows */
        using var reader =await command.ExecuteReaderAsync();
        if(await reader.ReadAsync())       // move to first row if exist
        {
            return MapReaderToStreamResponse(reader);   // if found map row into stream response
        }
        return null;    // if not found return null
    }

    /* update Method */
    public async Task<bool> UpdateStreamAsync(int streamId, UpdateStreamRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE streams 
            SET channel_name = @ChannelName,
                platform = @Platform,
                stream_title = @StreamTitle,
                embed_parent_domain = @EmbedParentDomain
            WHERE stream_id = @StreamId;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", streamId);
        command.Parameters.AddWithValue("@ChannelName", request.ChannelName.Trim());
        command.Parameters.AddWithValue("@Platform", request.Platform);
        command.Parameters.AddWithValue("@StreamTitle", request.StreamTitle);
        command.Parameters.AddWithValue("@EmbedParentDomain", request.EmbedParentDomain.Trim().ToLowerInvariant());

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /* delete Method */
    public async Task<bool> DeleteStreamAsync(int streamId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "DELETE FROM streams WHERE stream_id = @StreamId;";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StreamId", streamId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /* Transition Stream Status to Live */
    public async Task<int?> UpdateStreamLiveStatusAsync(string channelName, DateTimeOffset startedAt)
    {
        using var connection = new MySqlConnection(_connectionString); // instantiate db socket
        await connection.OpenAsync();                                   // open async connection

        /*
            update matching twitch stream to Live and return the primary key id
            for linkage inside webhook audit logs
            Enforce state transition: only 'Scheduled' streams can transition to 'Live'
        */
        const string sql = @"
            UPDATE streams 
            SET status = 'Live',
                started_at = @StartedAt
            WHERE LOWER(channel_name) = LOWER(@ChannelName)
                AND platform = 'Twitch'
                AND status = 'Scheduled';

            SELECT stream_id 
            FROM streams 
            WHERE LOWER(channel_name) = LOWER(@ChannelName) 
                AND platform = 'Twitch' 
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ChannelName", channelName.Trim()); // bind parameter to prevent sql injection
        command.Parameters.AddWithValue("@StartedAt", startedAt.UtcDateTime);      // persist as standard UTC datetime

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : null;
    }

    /* Transition Stream Status to Ended */
    public async Task<int?> UpdateStreamOfflineStatusAsync(string channelName)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        /*
            mark stream record as Ended and record current database UTC timestamp
            Enforce state transition: only 'Live' streams can transition to 'Ended'
        */
        const string sql = @"
            UPDATE streams 
            SET status = 'Ended',
                ended_at = UTC_TIMESTAMP()
            WHERE LOWER(channel_name) = LOWER(@ChannelName)
                AND platform = 'Twitch'
                AND status = 'Live';

            SELECT stream_id 
            FROM streams 
            WHERE LOWER(channel_name) = LOWER(@ChannelName) 
                AND platform = 'Twitch' 
            LIMIT 1;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ChannelName", channelName.Trim());

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : null;
    }
}