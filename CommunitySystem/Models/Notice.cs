using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.Models;

public class Notice
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(260)]
    public string? ImagePath { get; set; }

    [StringLength(260)]
    public string? AttachmentPath { get; set; }

    [StringLength(180)]
    public string? AttachmentName { get; set; }

    public bool IsPublished { get; set; } = true;
    public bool IsPinned { get; set; }
    public bool IsFeatured { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
