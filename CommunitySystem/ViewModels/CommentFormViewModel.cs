using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.ViewModels;

public class CommentFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    [Required]
    [Display(Name = "Comment")]
    [StringLength(1000)]
    public string Body { get; set; } = string.Empty;

    public string? PostTitle { get; set; }
}
