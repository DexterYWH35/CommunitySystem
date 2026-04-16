namespace CommunitySystem.Models;

public class PostLike
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Post? Post { get; set; }
    public ApplicationUser? User { get; set; }
}
