namespace CommunitySystem.Models.Marketplace;

public class MarketplaceItemImage
{
    public int Id { get; set; }

    public int MarketplaceItemId { get; set; }

    public MarketplaceItem? MarketplaceItem { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

