namespace CommunitySystem.Models;

public class ComplaintCaseLabel
{
    public int Id { get; set; }

    public int ComplaintCaseId { get; set; }

    public ComplaintCase? ComplaintCase { get; set; }

    public int ComplaintLabelId { get; set; }

    public ComplaintLabel? ComplaintLabel { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

