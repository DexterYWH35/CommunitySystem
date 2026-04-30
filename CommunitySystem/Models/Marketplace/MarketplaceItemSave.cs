namespace CommunitySystem.Models.Marketplace;

public class MarketplaceItemSave
{
    public int Id { get; set; }

    public int MarketplaceItemId { get; set; }

    public MarketplaceItem? MarketplaceItem { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

