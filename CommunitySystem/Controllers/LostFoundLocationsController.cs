using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class LostFoundLocationsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var locations = await dbContext.LostFoundLocationPresets
            .AsNoTracking()
            .OrderBy(location => location.DisplayOrder)
            .ThenBy(location => location.Name)
            .ToListAsync();

        return View(locations);
    }

    public IActionResult Create()
    {
        return View(new LostFoundLocationPresetFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LostFoundLocationPresetFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        dbContext.LostFoundLocationPresets.Add(new LostFoundLocationPreset
        {
            Name = viewModel.Name.Trim(),
            IsActive = viewModel.IsActive,
            DisplayOrder = viewModel.DisplayOrder,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var location = await dbContext.LostFoundLocationPresets.FindAsync(id.Value);
        if (location is null)
        {
            return NotFound();
        }

        return View(new LostFoundLocationPresetFormViewModel
        {
            Id = location.Id,
            Name = location.Name,
            IsActive = location.IsActive,
            DisplayOrder = location.DisplayOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LostFoundLocationPresetFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var location = await dbContext.LostFoundLocationPresets.FindAsync(id);
        if (location is null)
        {
            return NotFound();
        }

        location.Name = viewModel.Name.Trim();
        location.IsActive = viewModel.IsActive;
        location.DisplayOrder = viewModel.DisplayOrder;

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var location = await dbContext.LostFoundLocationPresets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        return location is null ? NotFound() : View(location);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var location = await dbContext.LostFoundLocationPresets.FindAsync(id);
        if (location is not null)
        {
            dbContext.LostFoundLocationPresets.Remove(location);
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
