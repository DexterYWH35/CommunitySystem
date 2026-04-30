using System.ComponentModel.DataAnnotations;
using CommunitySystem.Models;
using Microsoft.AspNetCore.Http;

namespace CommunitySystem.ViewModels;

public class LostFoundItemFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Other category")]
    public string? CustomCategory { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Location")]
    public string LocationDetails { get; set; } = string.Empty;

    [StringLength(160)]
    [Display(Name = "Other location")]
    public string? CustomLocationDetails { get; set; }

    [Required]
    [Display(Name = "Item type")]
    public LostFoundListingType ListingType { get; set; } = LostFoundListingType.Found;

    [Display(Name = "Date related to item")]
    [DataType(DataType.Date)]
    public DateTime? IncidentDateUtc { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Contact name")]
    public string ContactName { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Contact email")]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    [StringLength(30)]
    [Display(Name = "Contact phone")]
    [Phone]
    public string? ContactPhone { get; set; }

    [Display(Name = "Item image")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImagePath { get; set; }
}
