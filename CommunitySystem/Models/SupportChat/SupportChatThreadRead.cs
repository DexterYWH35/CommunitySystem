using CommunitySystem.Models;

namespace CommunitySystem.Models.SupportChat;

public class SupportChatThreadRead
{
    public int Id { get; set; }

    public int SupportChatThreadId { get; set; }

    public SupportChatThread? SupportChatThread { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime LastReadAtUtc { get; set; }
}

