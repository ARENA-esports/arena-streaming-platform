namespace ChatService.Entities;

public class ChatMute
{
    public long MuteId { get; set; }
    public int UserId { get; set; }
    public int MutedBy { get; set; }
    public string? Reason { get; set; }
    public DateTime MutedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
