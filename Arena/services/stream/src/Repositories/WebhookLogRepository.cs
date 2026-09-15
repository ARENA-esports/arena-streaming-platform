using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace StreamService.Repositories;

public class WebhookLogRepository : IWebhookLogRepository   // declare concrete repository class implementing interface contact
{
    private readonly string _connectionString;

    public WebhookLogRepository(IConfiguration configuration)   // dependency injection to constructor
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")  // retrieve sql connector string
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");    // ensure immediate application fail
    }

    /* Deduplication Lookup */
    public async Task<bool> MessageExistsAsync(string messageId)
    {
        using var connection = new MySqlConnection(_connectionString);  // instantiates the db socket
        await connection.OpenAsync();       // open async network connection
        const string sql = "SELECT COUNT(1) FROM webhook_message_logs WHERE message_id = @MessageId;";
        using var command = new MySqlCommand(sql, connection);  // create ADO.NET command object and connect it to sql connection
        //hard: Strongly typed parameter mapping prevents SQL injection on untrusted external headers
        command.Parameters.Add("@MessageId",MySqlDbType.VarChar,128).Value = messageId;// parameterize query
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    /* Audit Insertion */
    public async Task LogMessageAsync(string messageId,int? streamId,string messageType,string? subscriptionType,string? payloadHash)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO webhook_message_logs (
                message_id, stream_id, message_type, subscription_type, payload_hash
            ) VALUES (
                @MessageId, @StreamId, @MessageType, @SubscriptionType, @PayloadHash
            );";
        using var command = new MySqlCommand(sql,connection);
        //hard: Explicit column typing bounds memory consumption and prevents database buffer overflows
        command.Parameters.Add("@MessageId", MySqlDbType.VarChar, 128).Value = messageId;
        command.Parameters.Add("@StreamId", MySqlDbType.Int32).Value = (object?)streamId ?? DBNull.Value; // handle nullable fields,prevent runtime null exception crashes
        command.Parameters.Add("@MessageType", MySqlDbType.VarChar, 64).Value = messageType;
        command.Parameters.Add("@SubscriptionType", MySqlDbType.VarChar, 64).Value = (object?)subscriptionType ?? DBNull.Value;// handle nullable fields,prevent runtime null exception crashes
        command.Parameters.Add("@PayloadHash", MySqlDbType.VarChar, 64).Value = (object?)payloadHash ?? DBNull.Value;
        await command.ExecuteNonQueryAsync();
    }

    /* Atomic Insert-First Deduplication */
    public async Task<bool> TryLogMessageAsync(string messageId, int? streamId, string messageType, string? subscriptionType, string? payloadHash)
    {
        using var connection = new MySqlConnection(_connectionString); // instantiates the db socket
        await connection.OpenAsync();                                  // open async network connection

        const string sql = @"
            INSERT INTO webhook_message_logs (
                message_id, stream_id, message_type, subscription_type, payload_hash
            ) VALUES (
                @MessageId, @StreamId, @MessageType, @SubscriptionType, @PayloadHash
            );";

        using var command = new MySqlCommand(sql, connection);
       //hard comment: Primary key constraint on message_id provides atomic deduplication without distributed lock contention
        command.Parameters.Add("@MessageId", MySqlDbType.VarChar, 128).Value = messageId;                   // primary key constraint guarantees atomic uniqueness
        command.Parameters.Add("@StreamId", MySqlDbType.Int32).Value = (object?)streamId ?? DBNull.Value;   // handle nullable fields, prevent runtime null exception crashes
        command.Parameters.Add("@MessageType", MySqlDbType.VarChar, 64).Value = messageType;
        command.Parameters.Add("@SubscriptionType", MySqlDbType.VarChar, 64).Value = (object?)subscriptionType ?? DBNull.Value;    // handle nullable fields
        command.Parameters.Add("@PayloadHash", MySqlDbType.VarChar, 64).Value = (object?)payloadHash ?? DBNull.Value;

        try
        {
            await command.ExecuteNonQueryAsync();
            return true; // insert succeeded -> genuinely new message ID, proceed with processing
        }
        catch (MySqlException ex) when (ex.Number == 1062) // MySQL error 1062: ER_DUP_ENTRY (duplicate primary key)
        {
            return false; // duplicate delivery -> another request already claimed this message ID
        }
    }
    //hard: Mitigated CWE-400 Unbounded Storage Growth by pruning expired deduplication records older than retention threshold
    public async Task<int> PurgeExpiredLogsAsync(int retentionDays = 7)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            DELETE FROM webhook_message_logs 
            WHERE received_at < DATE_SUB(UTC_TIMESTAMP(), INTERVAL @RetentionDays DAY)
            LIMIT 5000;";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.Add("@RetentionDays", MySqlDbType.Int32).Value = retentionDays;

        return await command.ExecuteNonQueryAsync();
    }

}