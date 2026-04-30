namespace CommunitySystem.Models.Notifications;

public class UserNotification
{
    public int Id { get; set; }

    public string RecipientUserId { get; set; } = string.Empty;

    public ApplicationUser? RecipientUser { get; set; }

    public string? ActorUserId { get; set; }

    public ApplicationUser? ActorUser { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }
}

