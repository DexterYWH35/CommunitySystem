using CommunitySystem.Models;

namespace CommunitySystem.Models.SupportChat;

public class SupportChatMessage
{
    public int Id { get; set; }

    public int SupportChatThreadId { get; set; }

    public SupportChatThread? SupportChatThread { get; set; }

    public string SenderUserId { get; set; } = string.Empty;

    public ApplicationUser? SenderUser { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
