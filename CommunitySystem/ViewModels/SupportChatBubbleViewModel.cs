namespace CommunitySystem.ViewModels;

public class SupportChatBubbleViewModel
{
    public bool IsAdmin { get; init; }

    public bool HasUnread { get; init; }

    public string ReturnUrl { get; init; } = "/";
}

