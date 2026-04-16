namespace CommunitySystem.Models;

public class CommentLike
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Comment? Comment { get; set; }
    public ApplicationUser? User { get; set; }
}
