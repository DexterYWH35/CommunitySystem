using System.ComponentModel.DataAnnotations;
using CommunitySystem.Models;

namespace CommunitySystem.ViewModels;

public class ComplaintCaseAdminUpdateViewModel
{
    [Required]
    public int Id { get; set; }

    [Required]
    public ComplaintCaseStatus Status { get; set; }

    [StringLength(1500)]
    [Display(Name = "Remarks (admin only)")]
    public string? Remarks { get; set; }

    [Display(Name = "Labels")]
    public int[] SelectedLabelIds { get; set; } = Array.Empty<int>();

    [Display(Name = "New labels (comma separated)")]
    [StringLength(300)]
    public string? NewLabels { get; set; }
}

