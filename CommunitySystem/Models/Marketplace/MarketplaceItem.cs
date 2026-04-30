namespace CommunitySystem.Models.Marketplace;

public class MarketplaceItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public MarketplaceListingType ListingType { get; set; } = MarketplaceListingType.ForSale;

    public decimal Price { get; set; }

    public string? PaymentQrCodePath { get; set; }

    public bool IsActive { get; set; } = true;

    public string OwnerUserId { get; set; } = string.Empty;

    public ApplicationUser? OwnerUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<MarketplaceItemImage> Images { get; set; } = new List<MarketplaceItemImage>();
}

