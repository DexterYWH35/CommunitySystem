using CommunitySystem.Models;

namespace CommunitySystem.ViewModels;

public class HomeIndexViewModel
{
    public int TotalPosts { get; init; }
    public int TotalComments { get; init; }
    public string? LatestPostTitle { get; init; }
    public IReadOnlyCollection<Post> RecentPosts { get; init; } = Array.Empty<Post>();
    public IReadOnlyCollection<Post> MostLikedPosts { get; init; } = Array.Empty<Post>();
    public IReadOnlyCollection<Notice> Notices { get; init; } = Array.Empty<Notice>();
    public Notice? FeaturedNotice { get; init; }

    public bool IsAdmin { get; init; }
    public int LostFoundOpenCount { get; init; }
    public int LostFoundUnderReviewCount { get; init; }
    public int LostFoundResolvedCount { get; init; }
    public int LostFoundClaimsLast7Days { get; init; }
    public IReadOnlyCollection<LostFoundItem> RecentLostFoundItems { get; init; } = Array.Empty<LostFoundItem>();
}
