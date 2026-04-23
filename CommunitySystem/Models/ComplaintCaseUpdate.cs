namespace CommunitySystem.Models;

public class ComplaintCaseUpdate
{
    public int Id { get; set; }

    public int ComplaintCaseId { get; set; }

    public ComplaintCase? ComplaintCase { get; set; }

    public ComplaintCaseStatus Status { get; set; }

    public string? Remarks { get; set; }

    public string UpdatedByUserId { get; set; } = string.Empty;

    public ApplicationUser? UpdatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

