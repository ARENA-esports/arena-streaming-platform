using ChatService.Entities;

namespace ChatService.Repositories;

public interface IChatMuteRepository
{
    /// <summary>
    /// Inserts a new mute record into the chat_mutes table.
    /// </summary>
    /// <returns>The auto-generated mute_id of the inserted row.</returns>
    Task<long> InsertAsync(ChatMute mute);

    /// <summary>
    /// Checks whether a user currently has an active (non-expired) mute.
    /// </summary>
    Task<bool> IsUserMutedAsync(int userId);

    /// <summary>
    /// Retrieves the most recent active mute for a user, or null if not muted.
    /// </summary>
    Task<ChatMute?> GetActiveMuteAsync(int userId);
}
