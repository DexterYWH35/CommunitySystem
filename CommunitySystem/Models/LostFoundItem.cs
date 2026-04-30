using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.Models;

public class LostFoundItem
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

    [Required]
    [StringLength(160)]
    public string LocationDetails { get; set; } = string.Empty;

    [Display(Name = "Item type")]
    public LostFoundListingType ListingType { get; set; } = LostFoundListingType.Found;

    [Display(Name = "Current status")]
    public LostFoundItemStatus Status { get; set; } = LostFoundItemStatus.Open;

    [Display(Name = "Date related to item")]
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

    [StringLength(260)]
    public string? ImagePath { get; set; }

    public string? ReporterUserId { get; set; }
    public ApplicationUser? ReporterUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    public ICollection<LostFoundClaim> Claims { get; set; } = new List<LostFoundClaim>();

    public bool IsOpen => Status == LostFoundItemStatus.Open;
}
