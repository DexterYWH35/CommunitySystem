using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    [Required]
    [Display(Name = "Author")]
    [StringLength(80)]
    public string AuthorName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Comment")]
    [StringLength(1000)]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Created")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? OwnerUserId { get; set; }
    public ApplicationUser? OwnerUser { get; set; }

    public Post? Post { get; set; }
    public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
}
