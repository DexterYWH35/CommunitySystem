using CommunitySystem.Models;

namespace CommunitySystem.ViewModels;

public class ComplaintDashboardViewModel
{
    public int TotalCases { get; init; }

    public IReadOnlyDictionary<ComplaintCaseStatus, int> CasesByStatus { get; init; } =
        new Dictionary<ComplaintCaseStatus, int>();

    public List<ComplaintCase> RecentCases { get; init; } = new();
}

