using ChatService.Entities;

namespace ChatService.Repositories;

public interface IChatMessageRepository
{
    /// <summary>
    /// Persists a chat message to the chat_messages table using parameterized ADO.NET.
    /// </summary>
    /// <returns>The auto-generated message_id of the inserted row.</returns>
    Task<long> InsertAsync(ChatMessage message);
}
