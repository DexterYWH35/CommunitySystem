using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Models.SupportChat;
using CommunitySystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class SupportChatController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        if (User.IsInRole(RoleNames.Admin))
        {
            var threads = await dbContext.SupportChatThreads
                .AsNoTracking()
                .Include(thread => thread.User)
                .OrderByDescending(thread => thread.LastMessageAtUtc ?? thread.CreatedAtUtc)
                .Take(200)
                .ToListAsync();

            return View("AdminIndex", threads);
        }

        var threadId = await dbContext.SupportChatThreads
            .AsNoTracking()
            .Where(thread => thread.UserId == currentUserId)
            .Select(thread => (int?)thread.Id)
            .FirstOrDefaultAsync();

        if (threadId is not null)
        {
            return RedirectToAction(nameof(Thread), new { id = threadId.Value });
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Open(string? returnUrl = null)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var existingThreadId = await dbContext.SupportChatThreads
            .AsNoTracking()
            .Where(thread => thread.UserId == currentUserId)
            .Select(thread => (int?)thread.Id)
            .FirstOrDefaultAsync();

        if (existingThreadId is not null)
        {
            return RedirectToAction(nameof(Thread), new { id = existingThreadId.Value, returnUrl });
        }

        var thread = new SupportChatThread
        {
            UserId = currentUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.SupportChatThreads.Add(thread);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Thread), new { id = thread.Id, returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start()
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        if (User.IsInRole(RoleNames.Admin))
        {
            return Forbid();
        }

        var existingThreadId = await dbContext.SupportChatThreads
            .AsNoTracking()
            .Where(thread => thread.UserId == currentUserId)
            .Select(thread => (int?)thread.Id)
            .FirstOrDefaultAsync();

        if (existingThreadId is not null)
        {
            return RedirectToAction(nameof(Thread), new { id = existingThreadId.Value });
        }

        var thread = new SupportChatThread
        {
            UserId = currentUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.SupportChatThreads.Add(thread);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Thread), new { id = thread.Id });
    }

    public async Task<IActionResult> Thread(int id, string? returnUrl = null)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var thread = await dbContext.SupportChatThreads
            .AsNoTracking()
            .Include(value => value.User)
            .Include(value => value.Messages.OrderBy(message => message.CreatedAtUtc))
            .ThenInclude(message => message.SenderUser)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (thread is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.Admin) && thread.UserId != currentUserId)
        {
            return Forbid();
        }

        var now = DateTime.UtcNow;
        var read = await dbContext.SupportChatThreadReads
            .FirstOrDefaultAsync(value => value.SupportChatThreadId == id && value.UserId == currentUserId);

        if (read is null)
        {
            dbContext.SupportChatThreadReads.Add(new SupportChatThreadRead
            {
                SupportChatThreadId = id,
                UserId = currentUserId,
                LastReadAtUtc = now
            });
        }
        else if (read.LastReadAtUtc < now)
        {
            read.LastReadAtUtc = now;
        }

        await dbContext.SaveChangesAsync();

        ViewBag.CurrentUserId = currentUserId;
        ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View(thread);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int threadId, string? body)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var trimmed = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        if (trimmed is null)
        {
            return RedirectToAction(nameof(Thread), new { id = threadId });
        }

        var thread = await dbContext.SupportChatThreads.FirstOrDefaultAsync(value => value.Id == threadId);
        if (thread is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.Admin) && thread.UserId != currentUserId)
        {
            return Forbid();
        }

        var message = new SupportChatMessage
        {
            SupportChatThreadId = threadId,
            SenderUserId = currentUserId,
            Body = trimmed,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.SupportChatMessages.Add(message);
        thread.LastMessageAtUtc = message.CreatedAtUtc;
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Thread), new { id = threadId });
    }
}
