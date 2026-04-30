namespace CommunitySystem.Models.Marketplace;

public class MarketplaceChatThread
{
    public int Id { get; set; }

    public int MarketplaceItemId { get; set; }

    public MarketplaceItem? MarketplaceItem { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;

    public ApplicationUser? OwnerUser { get; set; }

    public string BuyerUserId { get; set; } = string.Empty;

    public ApplicationUser? BuyerUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAtUtc { get; set; }

    public ICollection<MarketplaceChatMessage> Messages { get; set; } = new List<MarketplaceChatMessage>();
}

