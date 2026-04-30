namespace CommunitySystem.Models.Marketplace;

public class MarketplaceChatMessage
{
    public int Id { get; set; }

    public int MarketplaceChatThreadId { get; set; }

    public MarketplaceChatThread? MarketplaceChatThread { get; set; }

    public string SenderUserId { get; set; } = string.Empty;

    public ApplicationUser? SenderUser { get; set; }

    public string Body { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
