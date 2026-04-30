using Microsoft.AspNetCore.Identity;
using CommunitySystem.Models.Marketplace;
using CommunitySystem.Models.Notifications;

namespace CommunitySystem.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    public ICollection<LostFoundItem> LostFoundItems { get; set; } = new List<LostFoundItem>();
    public ICollection<LostFoundClaim> LostFoundClaims { get; set; } = new List<LostFoundClaim>();
    public ICollection<ComplaintCase> ComplaintCases { get; set; } = new List<ComplaintCase>();
    public ICollection<MarketplaceItem> MarketplaceItems { get; set; } = new List<MarketplaceItem>();
    public ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
    public ICollection<MarketplaceItemSave> MarketplaceItemSaves { get; set; } = new List<MarketplaceItemSave>();
}
