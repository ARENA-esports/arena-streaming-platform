namespace ChatService.Entities;

public class ChatMessage
{
    public long MessageId { get; set; }
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
