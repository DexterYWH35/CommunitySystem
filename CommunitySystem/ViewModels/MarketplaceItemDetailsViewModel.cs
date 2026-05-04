using CommunitySystem.Models.Marketplace;

namespace CommunitySystem.ViewModels;

public class MarketplaceItemDetailsViewModel
{
    public required MarketplaceItem Item { get; init; }

    public bool CanManage { get; init; }

    public bool CanChat { get; init; }

    public int? ExistingChatThreadId { get; init; }
}
