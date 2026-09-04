namespace StreamService.Repositories;


public interface IWebhookLogRepository
{
    Task<bool> MessageExistsAsync(string messageId);    // verify unique Twitch message ID and detect duplicates
    Task LogMessageAsync(string messageId, int? streamId,string messageType,string? subscriptionType,string? payloadHash);  // insert dedup record along with audit metadata
}