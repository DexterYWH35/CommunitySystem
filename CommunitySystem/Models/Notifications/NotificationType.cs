namespace CommunitySystem.Models.Notifications;

public enum NotificationType
{
    // Keep numeric values stable (these are stored in SQLite).
    PostLiked = 1,
    CommentLiked = 2,
    ComplaintUpdated = 3,
    MarketplaceChatStarted = 4,
    MarketplaceMessage = 5,
    PostCommented = 6,
    LostFoundClaimSubmitted = 7,
    LostFoundStatusUpdated = 8
}
