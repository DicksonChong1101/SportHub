using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// Protects every action in this controller, including URLs entered manually.
[Authorize(Roles = UserRoles.Admin)]
public class FacilityCategoriesController : Controller
{
    private readonly AppDbContext _context;

    public FacilityCategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _context.FacilityCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();

        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var category = await _context.FacilityCategories
            .AsNoTracking()
            .Include(item => item.Facilities)
            .SingleOrDefaultAsync(item => item.FacilityCategoryId == id);

        return category is null ? NotFound() : View(category);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new FacilityCategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacilityCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedName = model.Name.Trim();

        if (await CategoryNameExistsAsync(normalizedName))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A facility category with this name already exists.");

            return View(model);
        }

        var category = new FacilityCategory
        {
            Name = normalizedName,
            Description = NormalizeOptionalText(model.Description),
            IsActive = model.IsActive
        };

        _context.FacilityCategories.Add(category);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The category could not be saved. Check that its name is unique and try again.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Facility category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.FacilityCategories.FindAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        var model = new FacilityCategoryFormViewModel
        {
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        FacilityCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = await _context.FacilityCategories.FindAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        var normalizedName = model.Name.Trim();

        if (await CategoryNameExistsAsync(normalizedName, id))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A facility category with this name already exists.");

            return View(model);
        }

        category.Name = normalizedName;
        category.Description = NormalizeOptionalText(model.Description);
        category.IsActive = model.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The category could not be updated. Check the submitted values and try again.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Facility category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.FacilityCategories
            .AsNoTracking()
            .Include(item => item.Facilities)
            .SingleOrDefaultAsync(item => item.FacilityCategoryId == id);

        return category is null ? NotFound() : View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.FacilityCategories
            .Include(item => item.Facilities)
            .SingleOrDefaultAsync(item => item.FacilityCategoryId == id);

        if (category is null)
        {
            return NotFound();
        }

        // Categories already used by facilities are retained to preserve related data.
        if (category.Facilities.Count > 0)
        {
            TempData["ErrorMessage"] =
                "This category is used by one or more facilities. Deactivate it instead of deleting it.";

            return RedirectToAction(nameof(Index));
        }

        _context.FacilityCategories.Remove(category);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The category could not be deleted because related records still exist.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Facility category deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> CategoryNameExistsAsync(
        string name,
        int? excludedCategoryId = null)
    {
        return _context.FacilityCategories.AnyAsync(category =>
            category.Name == name &&
            (!excludedCategoryId.HasValue ||
             category.FacilityCategoryId != excludedCategoryId.Value));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
