using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.ViewModels;

namespace SportHub.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class FacilitiesController : Controller
{
    private readonly AppDbContext _context;

    public FacilitiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? facilityType = null)
    {
        var selectedFacilityType = facilityType is FacilityTypes.Indoor or FacilityTypes.Outdoor
            ? facilityType
            : null;

        var query = _context.Facilities
            .AsNoTracking()
            .Include(facility => facility.FacilityCategory)
            .AsQueryable();

        if (selectedFacilityType is not null)
        {
            query = query.Where(facility => facility.FacilityType == selectedFacilityType);
        }

        var facilities = await query
            .OrderBy(facility => facility.Name)
            .ToListAsync();

        ViewData["FacilityType"] = selectedFacilityType;
        return View(facilities);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var facility = await _context.Facilities
            .AsNoTracking()
            .Include(item => item.FacilityCategory)
            .Include(item => item.FacilityImages)
            .Include(item => item.Reservations)
            .Include(item => item.MaintenanceBlocks)
            .SingleOrDefaultAsync(item => item.FacilityId == id);

        return facility is null ? NotFound() : View(facility);
    }

    // Loads the active categories and displays the facility creation form.
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new FacilityFormViewModel();
        await PopulateCategoryOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacilityFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoryOptionsAsync(model);
            return View(model);
        }

        var categoryIsAvailable = await _context.FacilityCategories
            .AnyAsync(category =>
                category.FacilityCategoryId == model.FacilityCategoryId &&
                category.IsActive);

        if (!categoryIsAvailable)
        {
            ModelState.AddModelError(
                nameof(model.FacilityCategoryId),
                "Select an active facility category.");
        }

        var normalizedName = model.Name.Trim();

        if (await FacilityNameExistsAsync(normalizedName))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A facility with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoryOptionsAsync(model);
            return View(model);
        }

        var facility = new Facility
        {
            Name = normalizedName,
            Description = NormalizeOptionalText(model.Description),
            Location = model.Location.Trim(),
            FacilityType = model.FacilityType,
            Capacity = model.Capacity,
            PricePerSlot = model.PricePerSlot,
            FacilityCategoryId = model.FacilityCategoryId,
            IsActive = model.IsActive
        };

        _context.Facilities.Add(facility);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The facility could not be saved. Check the submitted values and try again.");

            await PopulateCategoryOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Facility created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Loads the selected facility and prepares its existing values for editing.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var facility = await _context.Facilities.FindAsync(id);

        if (facility is null)
        {
            return NotFound();
        }

        var model = new FacilityFormViewModel
        {
            Name = facility.Name,
            Description = facility.Description,
            Location = facility.Location,
            FacilityType = facility.FacilityType,
            Capacity = facility.Capacity,
            PricePerSlot = facility.PricePerSlot,
            FacilityCategoryId = facility.FacilityCategoryId,
            IsActive = facility.IsActive
        };

        await PopulateCategoryOptionsAsync(model, facility.FacilityCategoryId);
        return View(model);
    }

    // Validates the submitted changes and updates the existing facility.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FacilityFormViewModel model)
    {
        var facility = await _context.Facilities.FindAsync(id);

        if (facility is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoryOptionsAsync(model, facility.FacilityCategoryId);
            return View(model);
        }

        var categoryIsAvailable = await _context.FacilityCategories
            .AnyAsync(category =>
                category.FacilityCategoryId == model.FacilityCategoryId &&
                (category.IsActive ||
                 category.FacilityCategoryId == facility.FacilityCategoryId));

        if (!categoryIsAvailable)
        {
            ModelState.AddModelError(
                nameof(model.FacilityCategoryId),
                "Select an active facility category.");
        }

        var normalizedName = model.Name.Trim();

        if (await FacilityNameExistsAsync(normalizedName, facility.FacilityId))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A facility with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoryOptionsAsync(model, facility.FacilityCategoryId);
            return View(model);
        }

        facility.Name = normalizedName;
        facility.Description = NormalizeOptionalText(model.Description);
        facility.Location = model.Location.Trim();
        facility.FacilityType = model.FacilityType;
        facility.Capacity = model.Capacity;
        facility.PricePerSlot = model.PricePerSlot;
        facility.FacilityCategoryId = model.FacilityCategoryId;
        facility.IsActive = model.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The facility could not be updated. Check the submitted values and try again.");

            await PopulateCategoryOptionsAsync(model, facility.FacilityCategoryId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Facility updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Loads the facility and its related record counts for delete confirmation.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var facility = await _context.Facilities
            .AsNoTracking()
            .Include(item => item.FacilityCategory)
            .Include(item => item.FacilityImages)
            .Include(item => item.Reservations)
            .Include(item => item.MaintenanceBlocks)
            .SingleOrDefaultAsync(item => item.FacilityId == id);

        return facility is null ? NotFound() : View(facility);
    }

    // Deletes an unused facility or deactivates one that has related records.
    // The ActionName keeps the form route as POST /Facilities/Delete/{id} while the C# method uses a distinct name.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var facility = await _context.Facilities
            .Include(item => item.FacilityImages)
            .Include(item => item.Reservations)
            .Include(item => item.MaintenanceBlocks)
            .SingleOrDefaultAsync(item => item.FacilityId == id);

        if (facility is null)
        {
            return NotFound();
        }

        var hasRelatedRecords =
            facility.FacilityImages.Count > 0 ||
            facility.Reservations.Count > 0 ||
            facility.MaintenanceBlocks.Count > 0;

        if (hasRelatedRecords)
        {
            // Preserve operational history by deactivating facilities that already have related records.
            facility.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "The facility could not be deactivated. Try again.";

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "This facility has related records and was deactivated instead of deleted.";

            return RedirectToAction(nameof(Index));
        }

        _context.Facilities.Remove(facility);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The facility could not be deleted because related records still exist.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Facility deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Lets Edit retain the facility's current category even if that category is inactive.
    private async Task PopulateCategoryOptionsAsync(
        FacilityFormViewModel model,
        int? selectedCategoryId = null)
    {
        model.CategoryOptions = await _context.FacilityCategories
            .AsNoTracking()
            .Where(category =>
                category.IsActive ||
                (selectedCategoryId.HasValue &&
                 category.FacilityCategoryId == selectedCategoryId.Value))
            .OrderBy(category => category.Name)
            .Select(category => new SelectListItem
            {
                Value = category.FacilityCategoryId.ToString(),
                Text = category.IsActive
                    ? category.Name
                    : $"{category.Name} (Inactive)",
                Selected = category.FacilityCategoryId == selectedCategoryId
            })
            .ToListAsync();
    }

    // Checks duplicate names while optionally excluding the facility currently being edited.
    private Task<bool> FacilityNameExistsAsync(
        string name,
        int? excludedFacilityId = null)
    {
        return _context.Facilities.AnyAsync(facility =>
            facility.Name == name &&
            (!excludedFacilityId.HasValue ||
             facility.FacilityId != excludedFacilityId.Value));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
