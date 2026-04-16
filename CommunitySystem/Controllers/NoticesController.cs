using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize]
public class NoticesController(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment) : Controller
{
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Index()
    {
        var notices = await dbContext.Notices
            .AsNoTracking()
            .OrderByDescending(notice => notice.IsFeatured)
            .ThenByDescending(notice => notice.IsPinned)
            .ThenByDescending(notice => notice.CreatedAtUtc)
            .ToListAsync();

        return View(notices);
    }

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var notice = await dbContext.Notices
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value && item.IsPublished);

        if (notice is null && User.IsInRole(RoleNames.Admin))
        {
            notice = await dbContext.Notices
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id.Value);
        }

        return notice is null ? NotFound() : View(notice);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult Create()
    {
        return View(new NoticeFormViewModel());
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NoticeFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var notice = new Notice
        {
            Title = viewModel.Title,
            Content = viewModel.Content,
            ImagePath = await SaveImageAsync(viewModel.ImageFile),
            AttachmentPath = await SaveAttachmentAsync(viewModel.AttachmentFile),
            AttachmentName = viewModel.AttachmentFile?.FileName,
            IsPublished = viewModel.IsPublished,
            IsPinned = viewModel.IsPinned,
            IsFeatured = viewModel.IsFeatured,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Notices.Add(notice);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var notice = await dbContext.Notices.FindAsync(id.Value);
        if (notice is null)
        {
            return NotFound();
        }

        return View(new NoticeFormViewModel
        {
            Id = notice.Id,
            Title = notice.Title,
            Content = notice.Content,
            ExistingImagePath = notice.ImagePath,
            ExistingAttachmentPath = notice.AttachmentPath,
            ExistingAttachmentName = notice.AttachmentName,
            IsPublished = notice.IsPublished,
            IsPinned = notice.IsPinned,
            IsFeatured = notice.IsFeatured
        });
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NoticeFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var notice = await dbContext.Notices.FindAsync(id);
        if (notice is null)
        {
            return NotFound();
        }

        notice.Title = viewModel.Title;
        notice.Content = viewModel.Content;
        notice.IsPublished = viewModel.IsPublished;
        notice.IsPinned = viewModel.IsPinned;
        notice.IsFeatured = viewModel.IsFeatured;
        notice.UpdatedAtUtc = DateTime.UtcNow;

        if (viewModel.ImageFile is not null)
        {
            DeleteStoredFile(notice.ImagePath);
            notice.ImagePath = await SaveImageAsync(viewModel.ImageFile);
        }

        if (viewModel.AttachmentFile is not null)
        {
            DeleteStoredFile(notice.AttachmentPath);
            notice.AttachmentPath = await SaveAttachmentAsync(viewModel.AttachmentFile);
            notice.AttachmentName = viewModel.AttachmentFile.FileName;
        }

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var notice = await dbContext.Notices
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        return notice is null ? NotFound() : View(notice);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var notice = await dbContext.Notices.FindAsync(id);
        if (notice is not null)
        {
            DeleteStoredFile(notice.ImagePath);
            DeleteStoredFile(notice.AttachmentPath);
            dbContext.Notices.Remove(notice);
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "notices");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(imageFile.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/notices/{fileName}";
    }

    private async Task<string?> SaveAttachmentAsync(IFormFile? attachmentFile)
    {
        if (attachmentFile is null || attachmentFile.Length == 0)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "notices", "attachments");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(attachmentFile.FileName);
        var safeExtension = string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) ? ".pdf" : ".pdf";
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await attachmentFile.CopyToAsync(stream);

        return $"/uploads/notices/attachments/{fileName}";
    }

    private void DeleteStoredFile(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return;
        }

        var trimmedPath = storedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webHostEnvironment.WebRootPath, trimmedPath);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
