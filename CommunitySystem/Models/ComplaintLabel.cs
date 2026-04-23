namespace CommunitySystem.Models;

public class ComplaintLabel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<ComplaintCaseLabel> Cases { get; set; } = new List<ComplaintCaseLabel>();
}

