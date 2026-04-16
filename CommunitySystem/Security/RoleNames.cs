namespace CommunitySystem.Security;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static IReadOnlyCollection<string> All { get; } = new[] { Admin, User };
}
