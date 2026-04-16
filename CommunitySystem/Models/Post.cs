using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.Models;

public class Post
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Author")]
    [StringLength(80)]
    public string AuthorName { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(260)]
    public string? ImagePath { get; set; }

    [Display(Name = "Created")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Display(Name = "Updated")]
    public DateTime? UpdatedAtUtc { get; set; }

    public string? OwnerUserId { get; set; }
    public ApplicationUser? OwnerUser { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

    public string ContentPreview =>
        Content.Length <= 140 ? Content : $"{Content[..140]}...";
}
