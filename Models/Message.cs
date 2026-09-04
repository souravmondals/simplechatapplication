// <author>Sourav Mondal/author>
// <summary>Chat application</summary>

namespace SimpleChat.Models;

public class Message
{
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    // Convenience field for the UI (sender's display name)
    public string? SenderName { get; set; }
}
