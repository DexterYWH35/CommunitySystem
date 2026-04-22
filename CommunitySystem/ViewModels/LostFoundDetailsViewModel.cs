using CommunitySystem.Models;

namespace CommunitySystem.ViewModels;

public class LostFoundDetailsViewModel
{
    public LostFoundItem Item { get; set; } = null!;
    public IReadOnlyList<LostFoundClaim> Claims { get; set; } = [];
    public LostFoundClaimFormViewModel ClaimForm { get; set; } = new();
    public bool HasCurrentUserClaimed { get; set; }
    public bool IsAdminView { get; set; }
    public bool CanManageItem { get; set; }
    public bool CanDeleteItem { get; set; }
}
