using CommunitySystem.Models.Notifications;

namespace CommunitySystem.Services.Notifications;

public interface INotificationService
{
    Task<UserNotification> CreateAsync(UserNotification notification, CancellationToken cancellationToken = default);
}

