using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CommunitySystem.ViewModels;

public class ComplaintCaseFormViewModel
{
    public const string OtherLabelValue = "__OTHER__";

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Complaint type")]
    public string LabelSelection { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Other complaint type")]
    public string? CustomLabelName { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Location")]
    public string LocationDetails { get; set; } = string.Empty;

    [StringLength(160)]
    [Display(Name = "Other location")]
    public string? CustomLocationDetails { get; set; }

    [Display(Name = "Images")]
    public IFormFile[]? ImageFiles { get; set; }
}
