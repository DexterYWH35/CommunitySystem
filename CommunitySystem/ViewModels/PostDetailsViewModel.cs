using CommunitySystem.Models;

namespace CommunitySystem.ViewModels;

public class PostDetailsViewModel
{
    public required Post Post { get; init; }
    public required IReadOnlyCollection<Comment> Comments { get; init; }
    public CommentFormViewModel NewComment { get; init; } = new();
    public int PostLikeCount { get; init; }
    public bool HasLikedPost { get; init; }
}
