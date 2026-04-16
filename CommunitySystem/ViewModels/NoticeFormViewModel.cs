using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CommunitySystem.ViewModels;

public class NoticeFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Notice image")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImagePath { get; set; }

    [Display(Name = "PDF attachment")]
    public IFormFile? AttachmentFile { get; set; }

    public string? ExistingAttachmentPath { get; set; }
    public string? ExistingAttachmentName { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "Pinned notice")]
    public bool IsPinned { get; set; }

    [Display(Name = "Featured notice")]
    public bool IsFeatured { get; set; }
}
