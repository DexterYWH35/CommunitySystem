using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class LostFoundController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment webHostEnvironment) : Controller
{
    private const string OtherLocationValue = "__OTHER__";

    public async Task<IActionResult> Index(LostFoundItemStatus? status = null, string? q = null)
    {
        var query = dbContext.LostFoundItems
            .AsNoTracking()
            .Include(item => item.Claims)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        var searchTerm = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        if (searchTerm is not null)
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(item =>
                EF.Functions.Like(item.Title, pattern) ||
                (item.Description != null && EF.Functions.Like(item.Description, pattern)) ||
                (item.Category != null && EF.Functions.Like(item.Category, pattern)) ||
                (item.LocationDetails != null && EF.Functions.Like(item.LocationDetails, pattern)) ||
                (item.ContactName != null && EF.Functions.Like(item.ContactName, pattern)) ||
                (item.ContactEmail != null && EF.Functions.Like(item.ContactEmail, pattern)) ||
                (item.ContactPhone != null && EF.Functions.Like(item.ContactPhone, pattern)));
        }

        var items = await query
            .OrderBy(item => item.Status == LostFoundItemStatus.Resolved)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ToListAsync();

        ViewData["SearchTerm"] = searchTerm;
        return View(items);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var viewModel = await BuildDetailsViewModelAsync(id.Value);
        SetClaimDropdowns();
        return viewModel is null ? NotFound() : View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLostFoundDropdownsAsync();
        return View(BuildItemFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LostFoundItemFormViewModel viewModel)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        await PopulateLostFoundDropdownsAsync();
        NormalizeLocationDetails(viewModel);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var item = new LostFoundItem
        {
            Title = viewModel.Title,
            Description = viewModel.Description,
            Category = viewModel.Category,
            LocationDetails = viewModel.LocationDetails,
            ListingType = viewModel.ListingType,
            IncidentDateUtc = viewModel.IncidentDateUtc?.Date,
            ContactName = viewModel.ContactName,
            ContactEmail = string.IsNullOrWhiteSpace(viewModel.ContactEmail) ? currentUser.Email : viewModel.ContactEmail,
            ContactPhone = viewModel.ContactPhone,
            ReporterUserId = currentUser.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ImagePath = await SaveImageAsync(viewModel.ImageFile)
        };

        dbContext.LostFoundItems.Add(item);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await dbContext.LostFoundItems.FindAsync(id.Value);
        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        await PopulateLostFoundDropdownsAsync();
        return View(BuildItemFormViewModel(new LostFoundItemFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Category = item.Category,
            LocationDetails = item.LocationDetails,
            ListingType = item.ListingType,
            IncidentDateUtc = item.IncidentDateUtc,
            ContactName = item.ContactName,
            ContactEmail = item.ContactEmail,
            ContactPhone = item.ContactPhone,
            ExistingImagePath = item.ImagePath
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LostFoundItemFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        await PopulateLostFoundDropdownsAsync();
        NormalizeLocationDetails(viewModel);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var item = await dbContext.LostFoundItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (!await CanManageItemAsync(item))
        {
            return Forbid();
        }

        item.Title = viewModel.Title;
        item.Description = viewModel.Description;
        item.Category = viewModel.Category;
        item.LocationDetails = viewModel.LocationDetails;
        item.ListingType = viewModel.ListingType;
        item.IncidentDateUtc = viewModel.IncidentDateUtc?.Date;
        item.ContactName = viewModel.ContactName;
        item.ContactEmail = viewModel.ContactEmail;
        item.ContactPhone = viewModel.ContactPhone;
        item.UpdatedAtUtc = DateTime.UtcNow;

        if (viewModel.ImageFile is not null)
        {
            DeleteStoredFile(item.ImagePath);
            item.ImagePath = await SaveImageAsync(viewModel.ImageFile);
        }

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, LostFoundItemStatus status)
    {
        var item = await dbContext.LostFoundItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = status;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await dbContext.LostFoundItems
            .AsNoTracking()
            .Include(existing => existing.Claims)
            .FirstOrDefaultAsync(existing => existing.Id == id.Value);

        if (item is null)
        {
            return NotFound();
        }

        if (!await CanDeleteItemAsync(item))
        {
            return Forbid();
        }

        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await dbContext.LostFoundItems
            .Include(existing => existing.Claims)
            .FirstOrDefaultAsync(existing => existing.Id == id);

        if (item is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!await CanDeleteItemAsync(item))
        {
            return Forbid();
        }

        DeleteStoredFile(item.ImagePath);
        dbContext.LostFoundItems.Remove(item);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(int id, LostFoundClaimFormViewModel claimForm)
    {
        if (id != claimForm.LostFoundItemId)
        {
            return NotFound();
        }

        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var item = await dbContext.LostFoundItems
            .Include(existing => existing.Claims)
            .FirstOrDefaultAsync(existing => existing.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (!item.IsOpen)
        {
            ModelState.AddModelError(string.Empty, "This item is no longer open for claims.");
        }

        var alreadyClaimed = await dbContext.LostFoundClaims
            .AnyAsync(claim => claim.LostFoundItemId == id && claim.ClaimerUserId == currentUser.Id);

        if (alreadyClaimed)
        {
            ModelState.AddModelError(string.Empty, "You have already submitted a claim for this item.");
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildDetailsViewModelAsync(id, claimForm);
            SetClaimDropdowns();
            return invalidViewModel is null ? NotFound() : View(nameof(Details), invalidViewModel);
        }

        dbContext.LostFoundClaims.Add(new LostFoundClaim
        {
            LostFoundItemId = id,
            ClaimerUserId = currentUser.Id,
            ClaimantName = claimForm.ClaimantName,
            ClaimantEmail = claimForm.ClaimantEmail,
            ClaimantPhone = claimForm.ClaimantPhone,
            VerificationDetails = claimForm.VerificationDetails,
            PreferredContactMethod = claimForm.PreferredContactMethod,
            CreatedAtUtc = DateTime.UtcNow
        });

        if (item.Status == LostFoundItemStatus.Open)
        {
            item.Status = LostFoundItemStatus.ClaimUnderReview;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    private LostFoundItemFormViewModel BuildItemFormViewModel(LostFoundItemFormViewModel? viewModel = null)
    {
        ViewBag.ListingTypeOptions = GetListingTypeOptions();
        return viewModel ?? new LostFoundItemFormViewModel();
    }

    private static IEnumerable<SelectListItem> GetListingTypeOptions()
    {
        return Enum.GetValues<LostFoundListingType>()
            .Select(value => new SelectListItem(value.ToString(), ((int)value).ToString()));
    }

    private void SetClaimDropdowns()
    {
        ViewBag.PreferredContactMethodOptions = new List<SelectListItem>
        {
            new("Select (optional)", ""),
            new("Phone call", "Phone"),
            new("WhatsApp", "WhatsApp"),
            new("Email", "Email")
        };
    }

    private async Task PopulateLostFoundDropdownsAsync()
    {
        ViewBag.ListingTypeOptions = GetListingTypeOptions();

        var locations = await dbContext.LostFoundLocationPresets
            .AsNoTracking()
            .Where(location => location.IsActive)
            .OrderBy(location => location.DisplayOrder)
            .ThenBy(location => location.Name)
            .Select(location => new SelectListItem(location.Name, location.Name))
            .ToListAsync();

        locations.Insert(0, new SelectListItem("Select a location", ""));
        locations.Add(new SelectListItem("Other (type below)", OtherLocationValue));
        ViewBag.LocationOptions = locations;
    }

    private void NormalizeLocationDetails(LostFoundItemFormViewModel viewModel)
    {
        if (string.Equals(viewModel.LocationDetails, OtherLocationValue, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(viewModel.CustomLocationDetails))
            {
                ModelState.AddModelError(nameof(viewModel.CustomLocationDetails), "Please enter the location.");
                return;
            }

            viewModel.LocationDetails = viewModel.CustomLocationDetails.Trim();
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.LocationDetails))
        {
            ModelState.AddModelError(nameof(viewModel.LocationDetails), "Please select a location.");
        }
    }

    private async Task<LostFoundDetailsViewModel?> BuildDetailsViewModelAsync(
        int id,
        LostFoundClaimFormViewModel? claimForm = null)
    {
        var item = await dbContext.LostFoundItems
            .AsNoTracking()
            .Include(existing => existing.Claims.OrderByDescending(claim => claim.CreatedAtUtc))
            .FirstOrDefaultAsync(existing => existing.Id == id);

        if (item is null)
        {
            return null;
        }

        var currentUserId = userManager.GetUserId(User);
        var isAdmin = User.IsInRole(RoleNames.Admin);
        var currentUser = currentUserId is null ? null : await userManager.GetUserAsync(User);

        return new LostFoundDetailsViewModel
        {
            Item = item,
            Claims = isAdmin ? item.Claims.OrderByDescending(claim => claim.CreatedAtUtc).ToList() : [],
            ClaimForm = claimForm ?? new LostFoundClaimFormViewModel
            {
                LostFoundItemId = item.Id,
                ClaimantEmail = currentUser?.Email ?? string.Empty,
                ClaimantPhone = currentUser?.PhoneNumber ?? string.Empty,
                ClaimantName = currentUser?.UserName ?? string.Empty
            },
            HasCurrentUserClaimed = currentUserId is not null &&
                                    item.Claims.Any(claim => claim.ClaimerUserId == currentUserId),
            IsAdminView = isAdmin,
            CanManageItem = isAdmin || (currentUserId is not null && item.ReporterUserId == currentUserId),
            CanDeleteItem = isAdmin ||
                            (currentUserId is not null &&
                             item.ReporterUserId == currentUserId &&
                             item.Claims.Count == 0)
        };
    }

    private async Task<bool> CanManageItemAsync(LostFoundItem item)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var currentUser = await userManager.GetUserAsync(User);
        return currentUser is not null && item.ReporterUserId == currentUser.Id;
    }

    private async Task<bool> CanDeleteItemAsync(LostFoundItem item)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var currentUser = await userManager.GetUserAsync(User);
        return currentUser is not null &&
               item.ReporterUserId == currentUser.Id &&
               !await dbContext.LostFoundClaims.AnyAsync(claim => claim.LostFoundItemId == item.Id);
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "lostfound");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(imageFile.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/lostfound/{fileName}";
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
