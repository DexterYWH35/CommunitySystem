using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class NotificationsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(bool unreadOnly = false)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var query = dbContext.UserNotifications
            .AsNoTracking()
            .Where(value => value.RecipientUserId == currentUserId)
            .Include(value => value.ActorUser)
            .OrderByDescending(value => value.CreatedAtUtc)
            .AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(value => !value.IsRead);
        }

        ViewBag.UnreadOnly = unreadOnly;

        var notifications = await query
            .Take(250)
            .ToListAsync();

        return View(notifications);
    }

    public async Task<IActionResult> Go(int id)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var notification = await dbContext.UserNotifications.FirstOrDefaultAsync(value => value.Id == id);
        if (notification is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.Admin) && notification.RecipientUserId != currentUserId)
        {
            return Forbid();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        if (string.IsNullOrWhiteSpace(notification.LinkUrl))
        {
            return RedirectToAction(nameof(Index), new { unreadOnly = false });
        }

        return Redirect(notification.LinkUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var unread = await dbContext.UserNotifications
            .Where(value => value.RecipientUserId == currentUserId && !value.IsRead)
            .ToListAsync();

        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { unreadOnly = false });
    }
}

