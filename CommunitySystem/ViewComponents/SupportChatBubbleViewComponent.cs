using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.ViewComponents;

public class SupportChatBubbleViewComponent(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var currentUserId = userManager.GetUserId((System.Security.Claims.ClaimsPrincipal)User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Content(string.Empty);
        }

        var isAdmin = ((System.Security.Claims.ClaimsPrincipal)User).IsInRole(RoleNames.Admin);
        var returnUrl = $"{HttpContext.Request.Path}{HttpContext.Request.QueryString}";

        var hasUnread = isAdmin
            ? await HasUnreadForAdminAsync(currentUserId)
            : await HasUnreadForUserAsync(currentUserId);

        return View(new SupportChatBubbleViewModel
        {
            IsAdmin = isAdmin,
            HasUnread = hasUnread,
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
        });
    }

    private async Task<bool> HasUnreadForUserAsync(string currentUserId)
    {
        var threadId = await dbContext.SupportChatThreads
            .AsNoTracking()
            .Where(value => value.UserId == currentUserId)
            .Select(value => (int?)value.Id)
            .FirstOrDefaultAsync();

        if (threadId is null)
        {
            return false;
        }

        var lastRead = await dbContext.SupportChatThreadReads
            .AsNoTracking()
            .Where(value => value.SupportChatThreadId == threadId.Value && value.UserId == currentUserId)
            .Select(value => (DateTime?)value.LastReadAtUtc)
            .FirstOrDefaultAsync();

        var cutoff = lastRead ?? DateTime.MinValue;
        return await dbContext.SupportChatMessages
            .AsNoTracking()
            .AnyAsync(message =>
                message.SupportChatThreadId == threadId.Value &&
                message.SenderUserId != currentUserId &&
                message.CreatedAtUtc > cutoff);
    }

    private async Task<bool> HasUnreadForAdminAsync(string currentUserId)
    {
        var query =
            from message in dbContext.SupportChatMessages.AsNoTracking()
            join read in dbContext.SupportChatThreadReads.AsNoTracking().Where(value => value.UserId == currentUserId)
                on new { ThreadId = message.SupportChatThreadId, UserId = currentUserId }
                equals new { ThreadId = read.SupportChatThreadId, read.UserId }
                into reads
            from read in reads.DefaultIfEmpty()
            where message.SenderUserId != currentUserId
            where message.CreatedAtUtc > (read == null ? DateTime.MinValue : read.LastReadAtUtc)
            select message.Id;

        return await query.AnyAsync();
    }
}
