using System.ComponentModel.DataAnnotations;

namespace CommunitySystem.ViewModels;

public class LostFoundLocationPresetFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Location name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }
}
