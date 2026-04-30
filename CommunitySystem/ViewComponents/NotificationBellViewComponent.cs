using CommunitySystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.ViewComponents;

public class NotificationBellViewComponent(ApplicationDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Content(string.Empty);
        }

        var unreadCount = await dbContext.UserNotifications
            .AsNoTracking()
            .CountAsync(value => value.RecipientUserId == userId && !value.IsRead);

        return View(unreadCount);
    }
}

