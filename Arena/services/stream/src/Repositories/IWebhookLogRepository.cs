namespace StreamService.Repositories;


public interface IWebhookLogRepository
{
    Task<bool> MessageExistsAsync(string messageId);    // verify unique Twitch message ID and detect duplicates
    Task LogMessageAsync(string messageId, int? streamId,string messageType,string? subscriptionType,string? payloadHash);  // insert dedup record along with audit metadata
    /*
        attempts atomic insertion of the webhook message ID.
        returns true if insertion succeeds (fresh message delivery).
        returns false if MySQL error 1062 occurs (duplicate delivery detected).
    */
    Task<bool> TryLogMessageAsync(string messageId, int? streamId, string messageType, string? subscriptionType, string? payloadHash);
}