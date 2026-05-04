using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Models.Marketplace;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class MarketplaceController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment webHostEnvironment) : Controller
{
    private const int MaxItemImages = 6;

    public async Task<IActionResult> Index(string? q, MarketplaceListingType? type)
    {
        var isAdmin = User.IsInRole(RoleNames.Admin);
        var query = dbContext.MarketplaceItems
            .AsNoTracking()
            .Where(item => isAdmin || item.IsActive)
            .Include(item => item.Images.OrderBy(image => image.Id))
            .Include(item => item.OwnerUser)
            .OrderByDescending(item => item.CreatedAtUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var trimmed = q.Trim();
            query = query.Where(item => item.Title.Contains(trimmed) || item.Description.Contains(trimmed));
        }

        if (type is not null)
        {
            query = query.Where(item => item.ListingType == type.Value);
        }

        ViewBag.SearchQuery = q;
        ViewBag.ListingType = type;

        var items = await query.Take(200).ToListAsync();

        var savedIds = new HashSet<int>();
        var currentUserId = userManager.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(currentUserId) && items.Count > 0)
        {
            var itemIds = items.Select(item => item.Id).ToList();
            savedIds = (await dbContext.MarketplaceItemSaves
                    .AsNoTracking()
                    .Where(save => save.UserId == currentUserId && itemIds.Contains(save.MarketplaceItemId))
                    .Select(save => save.MarketplaceItemId)
                    .ToListAsync())
                .ToHashSet();
        }

        return View(new MarketplaceIndexViewModel
        {
            Items = items,
            SavedItemIds = savedIds
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = userManager.GetUserId(User);
        var isAdmin = User.IsInRole(RoleNames.Admin);

        var item = await dbContext.MarketplaceItems
            .AsNoTracking()
            .Include(value => value.Images.OrderBy(image => image.Id))
            .Include(value => value.OwnerUser)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        var isOwner = currentUserId is not null && item.OwnerUserId == currentUserId;

        if (!item.IsActive && !isAdmin && !isOwner)
        {
            return NotFound();
        }

        var canManage = isAdmin || isOwner;
        var canChat = currentUserId is not null && item.OwnerUserId != currentUserId;

        int? existingThreadId = null;
        if (canChat)
        {
            existingThreadId = await dbContext.MarketplaceChatThreads
                .AsNoTracking()
                .Where(thread => thread.MarketplaceItemId == item.Id &&
                                 thread.OwnerUserId == item.OwnerUserId &&
                                 thread.BuyerUserId == currentUserId)
                .Select(thread => (int?)thread.Id)
                .FirstOrDefaultAsync();
        }

        return View(new MarketplaceItemDetailsViewModel
        {
            Item = item,
            CanManage = canManage,
            CanChat = canChat,
            ExistingChatThreadId = existingThreadId
        });
    }

    public IActionResult Create()
    {
        return View(new MarketplaceItemFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MarketplaceItemFormViewModel viewModel)
    {
        ValidateUploadedImages(viewModel.ImageFiles, nameof(viewModel.ImageFiles), MaxItemImages);
        ValidateUploadedQrCode(viewModel.PaymentQrCodeFile, nameof(viewModel.PaymentQrCodeFile));

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var item = new MarketplaceItem
        {
            Title = viewModel.Title.Trim(),
            Description = viewModel.Description.Trim(),
            ListingType = viewModel.ListingType,
            Price = viewModel.Price,
            OwnerUserId = currentUserId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.MarketplaceItems.Add(item);
        await dbContext.SaveChangesAsync();

        var storedImagePaths = await SaveItemImagesAsync(item.Id, viewModel.ImageFiles);
        foreach (var path in storedImagePaths)
        {
            dbContext.MarketplaceItemImages.Add(new MarketplaceItemImage
            {
                MarketplaceItemId = item.Id,
                ImagePath = path,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        item.PaymentQrCodePath = await SaveQrCodeAsync(item.Id, viewModel.PaymentQrCodeFile);

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    public async Task<IActionResult> MyListings()
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var items = await dbContext.MarketplaceItems
            .AsNoTracking()
            .Where(item => item.OwnerUserId == currentUserId)
            .Include(item => item.Images.OrderBy(image => image.Id))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync();

        return View(items);
    }

    public async Task<IActionResult> Saved()
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        var items = await dbContext.MarketplaceItemSaves
            .AsNoTracking()
            .Where(save => save.UserId == currentUserId)
            .Include(save => save.MarketplaceItem!)
            .ThenInclude(item => item.Images.OrderBy(image => image.Id))
            .Include(save => save.MarketplaceItem!)
            .ThenInclude(item => item.OwnerUser)
            .OrderByDescending(save => save.CreatedAtUtc)
            .Select(save => save.MarketplaceItem!)
            .Take(200)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSave(int id, string? returnUrl = null)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var itemExists = await dbContext.MarketplaceItems
            .AsNoTracking()
            .AnyAsync(item => item.Id == id && item.IsActive);

        if (!itemExists)
        {
            return NotFound();
        }

        var existing = await dbContext.MarketplaceItemSaves
            .FirstOrDefaultAsync(save => save.MarketplaceItemId == id && save.UserId == currentUserId);

        if (existing is null)
        {
            dbContext.MarketplaceItemSaves.Add(new MarketplaceItemSave
            {
                MarketplaceItemId = id,
                UserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.MarketplaceItemSaves.Remove(existing);
        }

        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await dbContext.MarketplaceItems
            .AsNoTracking()
            .Include(value => value.Images.OrderBy(image => image.Id))
            .Include(value => value.OwnerUser)
            .FirstOrDefaultAsync(value => value.Id == id.Value);

        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await dbContext.MarketplaceItems
            .Include(value => value.Images)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (item is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        foreach (var image in item.Images)
        {
            DeleteStoredFile(image.ImagePath);
        }

        DeleteStoredFile(item.PaymentQrCodePath);

        dbContext.MarketplaceItems.Remove(item);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(MyListings));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.MarketplaceItems
            .Include(value => value.Images.OrderBy(image => image.Id))
            .FirstOrDefaultAsync(value => value.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        var viewModel = new MarketplaceItemFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            ListingType = item.ListingType,
            Price = item.Price,
            IsActive = item.IsActive,
            ExistingPaymentQrCodePath = item.PaymentQrCodePath,
            ExistingImages = item.Images.Select(image => (image.Id, image.ImagePath)).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MarketplaceItemFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        var item = await dbContext.MarketplaceItems
            .Include(value => value.Images.OrderBy(image => image.Id))
            .FirstOrDefaultAsync(value => value.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        ValidateUploadedImages(viewModel.ImageFiles, nameof(viewModel.ImageFiles), MaxItemImages);
        ValidateUploadedQrCode(viewModel.PaymentQrCodeFile, nameof(viewModel.PaymentQrCodeFile));

        if (!ModelState.IsValid)
        {
            viewModel.ExistingPaymentQrCodePath = item.PaymentQrCodePath;
            viewModel.ExistingImages = item.Images.Select(image => (image.Id, image.ImagePath)).ToList();
            return View(viewModel);
        }

        item.Title = viewModel.Title.Trim();
        item.Description = viewModel.Description.Trim();
        item.ListingType = viewModel.ListingType;
        item.Price = viewModel.Price;
        item.IsActive = viewModel.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;

        var storedImagePaths = await SaveItemImagesAsync(item.Id, viewModel.ImageFiles);
        foreach (var path in storedImagePaths)
        {
            dbContext.MarketplaceItemImages.Add(new MarketplaceItemImage
            {
                MarketplaceItemId = item.Id,
                ImagePath = path,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (viewModel.PaymentQrCodeFile is not null)
        {
            DeleteStoredFile(item.PaymentQrCodePath);
            item.PaymentQrCodePath = await SaveQrCodeAsync(item.Id, viewModel.PaymentQrCodeFile);
        }

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = item.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var item = await dbContext.MarketplaceItems
            .Include(value => value.Images)
            .FirstOrDefaultAsync(value => value.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        var image = item.Images.FirstOrDefault(value => value.Id == imageId);
        if (image is null)
        {
            return NotFound();
        }

        DeleteStoredFile(image.ImagePath);
        dbContext.MarketplaceItemImages.Remove(image);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearPaymentQrCode(int id)
    {
        var item = await dbContext.MarketplaceItems.FirstOrDefaultAsync(value => value.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        DeleteStoredFile(item.PaymentQrCodePath);
        item.PaymentQrCodePath = null;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<bool> CanManageItemAsync(MarketplaceItem item)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var currentUser = await userManager.GetUserAsync(User);
        return currentUser is not null && item.OwnerUserId == currentUser.Id;
    }

    private void ValidateUploadedImages(IFormFile[]? imageFiles, string fieldName, int maxCount)
    {
        if (imageFiles is null || imageFiles.Length == 0)
        {
            return;
        }

        if (imageFiles.Length > maxCount)
        {
            ModelState.AddModelError(fieldName, $"You can upload up to {maxCount} images.");
            return;
        }

        foreach (var file in imageFiles)
        {
            if (file.Length == 0)
            {
                ModelState.AddModelError(fieldName, "One of the uploaded files is empty.");
                return;
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(fieldName, "Only image files are allowed.");
                return;
            }
        }
    }

    private void ValidateUploadedQrCode(IFormFile? qrCodeFile, string fieldName)
    {
        if (qrCodeFile is null)
        {
            return;
        }

        if (qrCodeFile.Length == 0)
        {
            ModelState.AddModelError(fieldName, "The uploaded QR code file is empty.");
            return;
        }

        if (!qrCodeFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(fieldName, "Only image files are allowed for the QR code.");
        }
    }

    private async Task<List<string>> SaveItemImagesAsync(int itemId, IFormFile[]? imageFiles)
    {
        var storedPaths = new List<string>();
        if (imageFiles is null || imageFiles.Length == 0)
        {
            return storedPaths;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "marketplace", itemId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        foreach (var imageFile in imageFiles)
        {
            var extension = Path.GetExtension(imageFile.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
            var fileName = $"img_{Guid.NewGuid():N}{safeExtension}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await imageFile.CopyToAsync(stream);

            storedPaths.Add($"/uploads/marketplace/{itemId}/{fileName}");
        }

        return storedPaths;
    }

    private async Task<string?> SaveQrCodeAsync(int itemId, IFormFile? qrCodeFile)
    {
        if (qrCodeFile is null || qrCodeFile.Length == 0)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "marketplace", itemId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(qrCodeFile.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        var fileName = $"qr_{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await qrCodeFile.CopyToAsync(stream);

        return $"/uploads/marketplace/{itemId}/{fileName}";
    }

    private void DeleteStoredFile(string? filePathOrUrlPath)
    {
        if (string.IsNullOrWhiteSpace(filePathOrUrlPath))
        {
            return;
        }

        var trimmedPath = filePathOrUrlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webHostEnvironment.WebRootPath, trimmedPath);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
