namespace CommunitySystem.Models;

public enum LostFoundListingType
{
    Lost = 1,
    Found = 2
}

public enum LostFoundItemStatus
{
    Open = 1,
    ClaimUnderReview = 2,
    Resolved = 3
}

public enum LostFoundClaimStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}
