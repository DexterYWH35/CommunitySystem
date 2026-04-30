using CommunitySystem.Data;
using CommunitySystem.Models.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Services.Notifications;

public class NotificationService(ApplicationDbContext dbContext) : INotificationService
{
    public async Task<UserNotification> CreateAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        dbContext.UserNotifications.Add(notification);

        // Keep a reasonable cap per user to avoid unbounded growth.
        var cutoff = DateTime.UtcNow.AddDays(-45);
        var oldIds = await dbContext.UserNotifications
            .AsNoTracking()
            .Where(value => value.RecipientUserId == notification.RecipientUserId && value.CreatedAtUtc < cutoff)
            .OrderBy(value => value.Id)
            .Select(value => value.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        if (oldIds.Count > 0)
        {
            var toRemove = await dbContext.UserNotifications
                .Where(value => oldIds.Contains(value.Id))
                .ToListAsync(cancellationToken);

            dbContext.UserNotifications.RemoveRange(toRemove);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }
}

