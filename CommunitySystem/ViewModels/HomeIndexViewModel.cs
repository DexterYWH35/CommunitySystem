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
}
