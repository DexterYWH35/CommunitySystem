using CommunitySystem.Models.Marketplace;

namespace CommunitySystem.ViewModels;

public class MarketplaceIndexViewModel
{
    public IReadOnlyCollection<MarketplaceItem> Items { get; init; } = Array.Empty<MarketplaceItem>();

    public ISet<int> SavedItemIds { get; init; } = new HashSet<int>();
}

