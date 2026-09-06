namespace StreamService.Services;

public interface IStreamStatusService
{
    /* mapping incoming twitch subscription types to status transitions */
    Task<int?> ProcessStreamStatusUpdateAsync(string subscriptionType,string channelName);
}