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
        command.Parameters.AddWithValue("@MessageId",messageId);// parameterize query
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
        command.Parameters.AddWithValue("@MessageId",messageId);
        command.Parameters.AddWithValue("@StreamId",(object?)streamId ?? DBNull.Value); // handle nullable fields,prevent runtime null exception crashes
        command.Parameters.AddWithValue("@MessageType",messageType);
        command.Parameters.AddWithValue("@SubscriptionType",(object?)subscriptionType ?? DBNull.Value);// handle nullable fields,prevent runtime null exception crashes
        command.Parameters.AddWithValue("@PayloadHash",(object?)payloadHash ?? DBNull.Value);
    }

}