using System.ComponentModel.DataAnnotations;
using CommunitySystem.Models.Marketplace;
using Microsoft.AspNetCore.Http;

namespace CommunitySystem.ViewModels;

public class MarketplaceItemFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Listing type")]
    public MarketplaceListingType ListingType { get; set; } = MarketplaceListingType.ForSale;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Display(Name = "Item images")]
    public IFormFile[]? ImageFiles { get; set; }

    [Display(Name = "Payment QR code (optional)")]
    public IFormFile? PaymentQrCodeFile { get; set; }

    public string? ExistingPaymentQrCodePath { get; set; }

    public List<(int Id, string Path)> ExistingImages { get; set; } = new();

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

