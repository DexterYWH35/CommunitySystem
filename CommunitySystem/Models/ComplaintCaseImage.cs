namespace CommunitySystem.Models;

public class ComplaintCaseImage
{
    public int Id { get; set; }

    public int ComplaintCaseId { get; set; }

    public ComplaintCase? ComplaintCase { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

