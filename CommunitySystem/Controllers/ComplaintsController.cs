using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Models.Notifications;
using CommunitySystem.Security;
using CommunitySystem.Services.Notifications;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class ComplaintsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment webHostEnvironment,
    INotificationService notificationService) : Controller
{
    private const string OtherLocationValue = "__OTHER__";
    private const int MaxImageUploads = 5;

    public async Task<IActionResult> Index(ComplaintCaseStatus? status = null, int? labelId = null, string? q = null)
    {
        var query = dbContext.ComplaintCases
            .AsNoTracking()
            .Include(item => item.Labels)
            .ThenInclude(link => link.ComplaintLabel)
            .AsQueryable();

        if (!User.IsInRole(RoleNames.Admin))
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser is null)
            {
                return Challenge();
            }

            query = query.Where(item => item.ReporterUserId == currentUser.Id);
        }

        if (status is not null)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (labelId is not null)
        {
            query = query.Where(item => item.Labels.Any(link => link.ComplaintLabelId == labelId.Value));
        }

        var searchTerm = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        if (searchTerm is not null)
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(item =>
                EF.Functions.Like(item.Title, pattern) ||
                (item.LocationDetails != null && EF.Functions.Like(item.LocationDetails, pattern)) ||
                (item.Description != null && EF.Functions.Like(item.Description, pattern)) ||
                item.Labels.Any(link => link.ComplaintLabel != null && EF.Functions.Like(link.ComplaintLabel.Name, pattern)));
        }

        var cases = await query
            .OrderBy(item => item.Status == ComplaintCaseStatus.Completed)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ToListAsync();

        ViewData["SearchTerm"] = searchTerm;
        await PopulateLabelDropdownsAsync(labelId);
        return View(cases);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var complaintCase = await dbContext.ComplaintCases
            .AsNoTracking()
            .Include(item => item.Images.OrderBy(image => image.Id))
            .Include(item => item.Labels.OrderBy(link => link.Id))
            .ThenInclude(link => link.ComplaintLabel)
            .Include(item => item.Updates.OrderByDescending(update => update.CreatedAtUtc))
            .ThenInclude(update => update.UpdatedByUser)
            .Include(item => item.ReporterUser)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (complaintCase is null)
        {
            return NotFound();
        }

        if (!await CanAccessCaseAsync(complaintCase))
        {
            return Forbid();
        }

        ViewBag.StatusOptions = GetStatusOptions(complaintCase.Status);
        await PopulateLabelDropdownsAsync(null, complaintCase.Labels.Select(link => link.ComplaintLabelId).ToArray());
        return View(complaintCase);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLocationDropdownsAsync();
        await PopulateLabelDropdownsForCreateAsync();
        return View(new ComplaintCaseFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplaintCaseFormViewModel viewModel)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        await PopulateLocationDropdownsAsync();
        await PopulateLabelDropdownsForCreateAsync(viewModel.LabelSelection);
        NormalizeLocationDetails(viewModel);
        var labelId = await NormalizeAndResolveLabelAsync(viewModel);
        ValidateUploadedImages(viewModel.ImageFiles);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var complaintCase = new ComplaintCase
        {
            Title = viewModel.Title,
            Description = viewModel.Description,
            LocationDetails = viewModel.LocationDetails,
            ReporterUserId = currentUser.Id,
            Status = ComplaintCaseStatus.Reviewing,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.ComplaintCases.Add(complaintCase);
        await dbContext.SaveChangesAsync();

        dbContext.ComplaintCaseLabels.Add(new ComplaintCaseLabel
        {
            ComplaintCaseId = complaintCase.Id,
            ComplaintLabelId = labelId,
            CreatedAtUtc = DateTime.UtcNow
        });

        var imagePaths = await SaveImagesAsync(complaintCase.Id, viewModel.ImageFiles);
        foreach (var imagePath in imagePaths)
        {
            dbContext.ComplaintCaseImages.Add(new ComplaintCaseImage
            {
                ComplaintCaseId = complaintCase.Id,
                ImagePath = imagePath,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = complaintCase.Id });
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Dashboard()
    {
        var totalCases = await dbContext.ComplaintCases.AsNoTracking().CountAsync();

        var casesByStatus = await dbContext.ComplaintCases
            .AsNoTracking()
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();

        var recentCases = await dbContext.ComplaintCases
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(10)
            .ToListAsync();

        var viewModel = new ComplaintDashboardViewModel
        {
            TotalCases = totalCases,
            CasesByStatus = casesByStatus.ToDictionary(item => item.Status, item => item.Count),
            RecentCases = recentCases
        };

        return View(viewModel);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(ComplaintCaseAdminUpdateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id = viewModel.Id });
        }

        var complaintCase = await dbContext.ComplaintCases
            .Include(item => item.Labels)
            .FirstOrDefaultAsync(item => item.Id == viewModel.Id);
        if (complaintCase is null)
        {
            return NotFound();
        }

        var currentAdmin = await userManager.GetUserAsync(User);
        if (currentAdmin is null)
        {
            return Challenge();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        complaintCase.Status = viewModel.Status;
        complaintCase.UpdatedAtUtc = DateTime.UtcNow;

        var remarks = string.IsNullOrWhiteSpace(viewModel.Remarks) ? null : viewModel.Remarks.Trim();
        dbContext.ComplaintCaseUpdates.Add(new ComplaintCaseUpdate
        {
            ComplaintCaseId = complaintCase.Id,
            Status = viewModel.Status,
            Remarks = remarks,
            UpdatedByUserId = currentAdmin.Id,
            CreatedAtUtc = DateTime.UtcNow
        });

        await UpdateCaseLabelsAsync(complaintCase, viewModel.SelectedLabelIds, viewModel.NewLabels);
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(complaintCase.ReporterUserId) && complaintCase.ReporterUserId != currentAdmin.Id)
        {
            var notificationTitle = viewModel.Status switch
            {
                ComplaintCaseStatus.Reviewing => "Your complaint is being reviewed",
                ComplaintCaseStatus.Completed => "Your complaint has been closed",
                _ => "Your complaint was updated"
            };

            var notificationBody = !string.IsNullOrWhiteSpace(remarks)
                ? remarks
                : $"Status: {viewModel.Status}";

            await notificationService.CreateAsync(new UserNotification
            {
                RecipientUserId = complaintCase.ReporterUserId,
                ActorUserId = currentAdmin.Id,
                Type = NotificationType.ComplaintUpdated,
                Title = notificationTitle,
                Body = notificationBody,
                LinkUrl = Url.Action("Details", "Complaints", new { id = complaintCase.Id })
            });
        }

        await transaction.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = complaintCase.Id });
    }

    private async Task<bool> CanAccessCaseAsync(ComplaintCase complaintCase)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var currentUser = await userManager.GetUserAsync(User);
        return currentUser is not null && complaintCase.ReporterUserId == currentUser.Id;
    }

    private async Task PopulateLocationDropdownsAsync()
    {
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

    private async Task PopulateLabelDropdownsAsync(int? selectedLabelId, int[]? selectedLabelIds = null)
    {
        var labels = await dbContext.ComplaintLabels
            .AsNoTracking()
            .OrderBy(label => label.Name)
            .Select(label => new SelectListItem(label.Name, label.Id.ToString()))
            .ToListAsync();

        var filterOptions = new List<SelectListItem>
        {
            new("All labels", "")
        };

        filterOptions.AddRange(labels);

        if (selectedLabelId is not null)
        {
            foreach (var option in filterOptions)
            {
                option.Selected = string.Equals(option.Value, selectedLabelId.Value.ToString(), StringComparison.Ordinal);
            }
        }

        ViewBag.LabelFilterOptions = filterOptions;
        ViewBag.LabelOptions = labels;
        ViewBag.SelectedLabelIds = selectedLabelIds ?? Array.Empty<int>();
    }

    private async Task PopulateLabelDropdownsForCreateAsync(string? selectedValue = null)
    {
        var labels = await dbContext.ComplaintLabels
            .AsNoTracking()
            .OrderBy(label => label.Name)
            .Select(label => new SelectListItem(label.Name, label.Id.ToString()))
            .ToListAsync();

        labels.Insert(0, new SelectListItem("Select a complaint type", ""));
        labels.Add(new SelectListItem("Other (type below)", ComplaintCaseFormViewModel.OtherLabelValue));

        if (!string.IsNullOrWhiteSpace(selectedValue))
        {
            foreach (var option in labels)
            {
                option.Selected = string.Equals(option.Value, selectedValue, StringComparison.Ordinal);
            }
        }

        ViewBag.CaseLabelOptions = labels;
    }

    private async Task<int> NormalizeAndResolveLabelAsync(ComplaintCaseFormViewModel viewModel)
    {
        if (string.Equals(viewModel.LabelSelection, ComplaintCaseFormViewModel.OtherLabelValue, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(viewModel.CustomLabelName))
            {
                ModelState.AddModelError(nameof(viewModel.CustomLabelName), "Please enter the complaint type.");
                return 0;
            }

            var normalizedName = viewModel.CustomLabelName.Trim();
            viewModel.CustomLabelName = normalizedName;

            var existing = await dbContext.ComplaintLabels
                .FirstOrDefaultAsync(label => label.Name.ToLower() == normalizedName.ToLower());

            if (existing is null)
            {
                existing = new ComplaintLabel
                {
                    Name = normalizedName,
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.ComplaintLabels.Add(existing);
                await dbContext.SaveChangesAsync();
            }

            return existing.Id;
        }

        if (string.IsNullOrWhiteSpace(viewModel.LabelSelection))
        {
            ModelState.AddModelError(nameof(viewModel.LabelSelection), "Please select a complaint type.");
            return 0;
        }

        if (!int.TryParse(viewModel.LabelSelection, out var labelId))
        {
            ModelState.AddModelError(nameof(viewModel.LabelSelection), "Invalid complaint type selection.");
            return 0;
        }

        var label = await dbContext.ComplaintLabels.FindAsync(labelId);
        if (label is null)
        {
            ModelState.AddModelError(nameof(viewModel.LabelSelection), "Selected complaint type not found.");
            return 0;
        }

        return label.Id;
    }

    private async Task UpdateCaseLabelsAsync(ComplaintCase complaintCase, int[] selectedLabelIds, string? newLabelsRaw)
    {
        var desiredLabelIds = new HashSet<int>(selectedLabelIds ?? Array.Empty<int>());

        foreach (var labelName in ParseNewLabels(newLabelsRaw))
        {
            var existing = await dbContext.ComplaintLabels
                .FirstOrDefaultAsync(label => label.Name.ToLower() == labelName.ToLower());

            if (existing is null)
            {
                existing = new ComplaintLabel
                {
                    Name = labelName,
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.ComplaintLabels.Add(existing);
                await dbContext.SaveChangesAsync();
            }

            desiredLabelIds.Add(existing.Id);
        }

        var existingLabelIds = complaintCase.Labels.Select(link => link.ComplaintLabelId).ToHashSet();

        var toRemove = complaintCase.Labels.Where(link => !desiredLabelIds.Contains(link.ComplaintLabelId)).ToList();
        if (toRemove.Count > 0)
        {
            dbContext.ComplaintCaseLabels.RemoveRange(toRemove);
        }

        var toAdd = desiredLabelIds.Where(id => !existingLabelIds.Contains(id)).ToList();
        foreach (var labelId in toAdd)
        {
            dbContext.ComplaintCaseLabels.Add(new ComplaintCaseLabel
            {
                ComplaintCaseId = complaintCase.Id,
                ComplaintLabelId = labelId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static IReadOnlyList<string> ParseNewLabels(string? newLabelsRaw)
    {
        if (string.IsNullOrWhiteSpace(newLabelsRaw))
        {
            return Array.Empty<string>();
        }

        return newLabelsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private void NormalizeLocationDetails(ComplaintCaseFormViewModel viewModel)
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

    private void ValidateUploadedImages(IFormFile[]? imageFiles)
    {
        if (imageFiles is null || imageFiles.Length == 0)
        {
            return;
        }

        if (imageFiles.Length > MaxImageUploads)
        {
            ModelState.AddModelError(nameof(ComplaintCaseFormViewModel.ImageFiles), $"You can upload up to {MaxImageUploads} images.");
            return;
        }

        foreach (var file in imageFiles)
        {
            if (file.Length == 0)
            {
                ModelState.AddModelError(nameof(ComplaintCaseFormViewModel.ImageFiles), "One of the uploaded files is empty.");
                return;
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ComplaintCaseFormViewModel.ImageFiles), "Only image files are allowed.");
                return;
            }
        }
    }

    private async Task<List<string>> SaveImagesAsync(int complaintCaseId, IFormFile[]? imageFiles)
    {
        var storedPaths = new List<string>();
        if (imageFiles is null || imageFiles.Length == 0)
        {
            return storedPaths;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "complaints", complaintCaseId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        foreach (var imageFile in imageFiles)
        {
            var extension = Path.GetExtension(imageFile.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
            var fileName = $"{Guid.NewGuid():N}{safeExtension}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await imageFile.CopyToAsync(stream);

            storedPaths.Add($"/uploads/complaints/{complaintCaseId}/{fileName}");
        }

        return storedPaths;
    }

    private static IEnumerable<SelectListItem> GetStatusOptions(ComplaintCaseStatus? selectedStatus = null)
    {
        return Enum.GetValues<ComplaintCaseStatus>()
            .Select(value => new SelectListItem(value.ToString(), ((int)value).ToString())
            {
                Selected = selectedStatus is not null && value == selectedStatus.Value
            });
    }
}
