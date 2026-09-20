using ChatService.Entities;

namespace ChatService.Repositories;

public interface IChatMessageRepository
{
    /// <summary>
    /// Persists a chat message to the chat_messages table using parameterized ADO.NET.
    /// </summary>
    /// <returns>The auto-generated message_id of the inserted row.</returns>
    Task<long> InsertAsync(ChatMessage message);

    /// <summary>
    /// Retrieves the most recent messages for a team, ordered chronologically (oldest first).
    /// Uses the existing composite index (team_id, created_at DESC) for efficient retrieval.
    /// </summary>
    /// <param name="teamId">The faction team ID to fetch messages for.</param>
    /// <param name="limit">Maximum number of messages to return (default 50).</param>
    Task<IEnumerable<ChatMessage>> GetRecentByTeamAsync(int teamId, int limit = 50);
}
