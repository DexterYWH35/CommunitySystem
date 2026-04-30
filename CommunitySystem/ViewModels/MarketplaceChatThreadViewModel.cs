using CommunitySystem.Models.Marketplace;

namespace CommunitySystem.ViewModels;

public class MarketplaceChatThreadViewModel
{
    public required MarketplaceChatThread Thread { get; init; }

    public required MarketplaceItem Item { get; init; }

    public required string CurrentUserId { get; init; }

    public required string OtherUserDisplayName { get; init; }
}

