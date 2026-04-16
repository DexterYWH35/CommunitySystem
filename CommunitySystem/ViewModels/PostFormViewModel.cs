using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CommunitySystem.ViewModels;

public class PostFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Post image")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImagePath { get; set; }
}
