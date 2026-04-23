namespace CommunitySystem.Models;

public class ComplaintCase
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LocationDetails { get; set; } = string.Empty;

    public ComplaintCaseStatus Status { get; set; } = ComplaintCaseStatus.Reviewing;

    public string ReporterUserId { get; set; } = string.Empty;

    public ApplicationUser? ReporterUser { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ComplaintCaseImage> Images { get; set; } = new List<ComplaintCaseImage>();

    public ICollection<ComplaintCaseLabel> Labels { get; set; } = new List<ComplaintCaseLabel>();

    public ICollection<ComplaintCaseUpdate> Updates { get; set; } = new List<ComplaintCaseUpdate>();
}
