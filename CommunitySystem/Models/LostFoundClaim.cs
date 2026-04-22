using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.Models;

public class LostFoundClaim
{
    public int Id { get; set; }

    public int LostFoundItemId { get; set; }
    public LostFoundItem? LostFoundItem { get; set; }

    public string? ClaimerUserId { get; set; }
    public ApplicationUser? ClaimerUser { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Full name")]
    public string ClaimantName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Display(Name = "Email address")]
    [EmailAddress]
    public string ClaimantEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Phone number")]
    [Phone]
    public string ClaimantPhone { get; set; } = string.Empty;

    [Required]
    [StringLength(1500)]
    [Display(Name = "Verification details")]
    public string VerificationDetails { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "Preferred contact method")]
    public string? PreferredContactMethod { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
