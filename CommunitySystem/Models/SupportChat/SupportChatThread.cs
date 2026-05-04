using CommunitySystem.Models;

namespace CommunitySystem.Models.SupportChat;

public class SupportChatThread
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastMessageAtUtc { get; set; }

    public ICollection<SupportChatMessage> Messages { get; set; } = new List<SupportChatMessage>();
}
