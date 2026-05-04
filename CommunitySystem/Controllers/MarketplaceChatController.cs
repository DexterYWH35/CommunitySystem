using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Models.Marketplace;
using CommunitySystem.Models.Notifications;
using CommunitySystem.Security;
using CommunitySystem.Services.Notifications;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class MarketplaceChatController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    INotificationService notificationService,
    IWebHostEnvironment webHostEnvironment) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var threads = await dbContext.MarketplaceChatThreads
            .AsNoTracking()
            .Where(thread => thread.OwnerUserId == currentUserId || thread.BuyerUserId == currentUserId)
            .Include(thread => thread.MarketplaceItem)
            .Include(thread => thread.OwnerUser)
            .Include(thread => thread.BuyerUser)
            .OrderByDescending(thread => thread.LastMessageAtUtc ?? thread.CreatedAtUtc)
            .Take(200)
            .ToListAsync();

        ViewBag.CurrentUserId = currentUserId;
        return View(threads);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int itemId)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole(RoleNames.Admin);
        var item = await dbContext.MarketplaceItems
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == itemId && (isAdmin || value.IsActive));

        if (item is null)
        {
            return NotFound();
        }

        if (item.OwnerUserId == currentUserId)
        {
            return RedirectToAction(nameof(Index));
        }

        var existingThread = await dbContext.MarketplaceChatThreads
            .FirstOrDefaultAsync(thread => thread.MarketplaceItemId == itemId &&
                                           thread.OwnerUserId == item.OwnerUserId &&
                                           thread.BuyerUserId == currentUserId);

        if (existingThread is null)
        {
            existingThread = new MarketplaceChatThread
            {
                MarketplaceItemId = itemId,
                OwnerUserId = item.OwnerUserId,
                BuyerUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.MarketplaceChatThreads.Add(existingThread);
            await dbContext.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(item.OwnerUserId) && item.OwnerUserId != currentUserId)
            {
                if (!await IsAdminUserIdAsync(item.OwnerUserId))
                {
                    await notificationService.CreateAsync(new UserNotification
                    {
                        RecipientUserId = item.OwnerUserId,
                        ActorUserId = currentUserId,
                        Type = NotificationType.MarketplaceChatStarted,
                        Title = "New marketplace chat",
                        Body = item.Title.Length > 100 ? item.Title[..100] + "…" : item.Title,
                        LinkUrl = Url.Action("Thread", "MarketplaceChat", new { id = existingThread.Id })
                    });
                }
            }
        }

        return RedirectToAction(nameof(Thread), new { id = existingThread.Id });
    }

    public async Task<IActionResult> Thread(int id)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var thread = await dbContext.MarketplaceChatThreads
            .AsNoTracking()
            .Include(value => value.MarketplaceItem)
            .Include(value => value.OwnerUser)
            .Include(value => value.BuyerUser)
            .Include(value => value.Messages.OrderBy(message => message.CreatedAtUtc))
            .ThenInclude(message => message.SenderUser)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (thread is null)
        {
            return NotFound();
        }

        if (thread.OwnerUserId != currentUserId && thread.BuyerUserId != currentUserId && !User.IsInRole(RoleNames.Admin))
        {
            return Forbid();
        }

        var item = thread.MarketplaceItem;
        if (item is null)
        {
            return NotFound();
        }

        var otherUser = currentUserId == thread.OwnerUserId ? thread.BuyerUser : thread.OwnerUser;
        var otherDisplayName = otherUser?.UserName ?? otherUser?.Email ?? "User";

        return View(new MarketplaceChatThreadViewModel
        {
            Thread = thread,
            Item = item,
            CurrentUserId = currentUserId,
            OtherUserDisplayName = otherDisplayName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int threadId, string? body, IFormFile? imageFile)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var trimmedBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        ValidateUploadedImage(imageFile);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (trimmedBody is null && imageFile is null)
        {
            return BadRequest();
        }

        var thread = await dbContext.MarketplaceChatThreads
            .FirstOrDefaultAsync(value => value.Id == threadId);

        if (thread is null)
        {
            return NotFound();
        }

        if (thread.OwnerUserId != currentUserId && thread.BuyerUserId != currentUserId && !User.IsInRole(RoleNames.Admin))
        {
            return Forbid();
        }

        var imagePath = await SaveChatImageAsync(threadId, imageFile);

        var message = new MarketplaceChatMessage
        {
            MarketplaceChatThreadId = threadId,
            SenderUserId = currentUserId,
            Body = trimmedBody ?? string.Empty,
            ImagePath = imagePath,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.MarketplaceChatMessages.Add(message);
        thread.LastMessageAtUtc = message.CreatedAtUtc;
        await dbContext.SaveChangesAsync();

        var recipientUserId = currentUserId == thread.OwnerUserId ? thread.BuyerUserId : thread.OwnerUserId;
        if (!string.IsNullOrWhiteSpace(recipientUserId) && recipientUserId != currentUserId)
        {
            if (!await IsAdminUserIdAsync(recipientUserId))
            {
                var itemTitle = await dbContext.MarketplaceItems
                    .AsNoTracking()
                    .Where(item => item.Id == thread.MarketplaceItemId)
                    .Select(item => item.Title)
                    .FirstOrDefaultAsync();

                await notificationService.CreateAsync(new UserNotification
                {
                    RecipientUserId = recipientUserId,
                    ActorUserId = currentUserId,
                    Type = NotificationType.MarketplaceMessage,
                    Title = "New marketplace message",
                    Body = itemTitle,
                    LinkUrl = Url.Action("Thread", "MarketplaceChat", new { id = threadId })
                });
            }
        }

        if (IsAjaxRequest(Request))
        {
            return Json(new
            {
                ok = true,
                messageId = message.Id,
                createdAtUtc = message.CreatedAtUtc
            });
        }

        return RedirectToAction(nameof(Thread), new { id = threadId });
    }

    public async Task<IActionResult> Messages(int threadId, int afterId = 0)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var thread = await dbContext.MarketplaceChatThreads
            .AsNoTracking()
            .Select(value => new { value.Id, value.OwnerUserId, value.BuyerUserId })
            .FirstOrDefaultAsync(value => value.Id == threadId);

        if (thread is null)
        {
            return NotFound();
        }

        if (thread.OwnerUserId != currentUserId && thread.BuyerUserId != currentUserId && !User.IsInRole(RoleNames.Admin))
        {
            return Forbid();
        }

        var messages = await dbContext.MarketplaceChatMessages
            .AsNoTracking()
            .Where(message => message.MarketplaceChatThreadId == threadId && message.Id > afterId)
            .Include(message => message.SenderUser)
            .OrderBy(message => message.Id)
            .Take(200)
            .ToListAsync();

        return Json(messages.Select(message => new
        {
            id = message.Id,
            senderUserId = message.SenderUserId,
            senderName = message.SenderUser?.UserName ?? "User",
            body = message.Body,
            imageUrl = message.ImagePath,
            createdAtUtc = message.CreatedAtUtc
        }));
    }

    private void ValidateUploadedImage(IFormFile? imageFile)
    {
        if (imageFile is null)
        {
            return;
        }

        if (imageFile.Length == 0)
        {
            ModelState.AddModelError(nameof(imageFile), "The uploaded image is empty.");
            return;
        }

        if (!imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(imageFile), "Only image files are allowed.");
        }
    }

    private async Task<string?> SaveChatImageAsync(int threadId, IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "marketplace", "chat", threadId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(imageFile.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        var fileName = $"msg_{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/marketplace/chat/{threadId}/{fileName}";
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsAdminUserIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsInRoleAsync(user, RoleNames.Admin);
    }
}
