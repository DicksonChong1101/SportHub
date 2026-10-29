using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.ViewModels;

namespace SportHub.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class MaintenanceBlocksController : Controller
{
    private readonly AppDbContext _context;

    public MaintenanceBlocksController(AppDbContext context)
    {
        _context = context;
    }

    // Displays all maintenance blocks.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var blocks = await _context.MaintenanceBlocks
            .AsNoTracking()
            .Include(item => item.Facility)
            .Include(item => item.CreatedByUser)
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.StartAt)
            .ToListAsync();

        return View(blocks);
    }

    // Displays one maintenance block.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var block = await MaintenanceBlockQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.MaintenanceBlockId == id);

        return block is null ? NotFound() : View(block);
    }

    // Displays the Create form.
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var startAt = DateTime.Today.AddDays(1).AddHours(9);
        var model = new MaintenanceBlockFormViewModel
        {
            StartAt = startAt,
            EndAt = startAt.AddHours(1)
        };

        await PopulateFacilityOptionsAsync(model);
        return View(model);
    }

    // Receives and validates the Create form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceBlockFormViewModel model)
    {
        await ValidateMaintenanceBlockAsync(model);

        if (!ModelState.IsValid)
        {
            await PopulateFacilityOptionsAsync(model);
            return View(model);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var block = new MaintenanceBlock
        {
            FacilityId = model.FacilityId,
            CreatedByUserId = userId,
            StartAt = model.StartAt!.Value,
            EndAt = model.EndAt!.Value,
            Reason = model.Reason.Trim(),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.MaintenanceBlocks.Add(block);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The maintenance block could not be saved. Check the submitted values and try again.");

            await PopulateFacilityOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Maintenance block created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Displays the Edit form.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var block = await _context.MaintenanceBlocks.FindAsync(id);

        if (block is null)
        {
            return NotFound();
        }

        var model = new MaintenanceBlockFormViewModel
        {
            FacilityId = block.FacilityId,
            StartAt = block.StartAt,
            EndAt = block.EndAt,
            Reason = block.Reason,
            IsActive = block.IsActive
        };

        await PopulateFacilityOptionsAsync(model, block.FacilityId);
        return View(model);
    }

    // Receives and validates the Edit form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MaintenanceBlockFormViewModel model)
    {
        var block = await _context.MaintenanceBlocks.FindAsync(id);

        if (block is null)
        {
            return NotFound();
        }

        await ValidateMaintenanceBlockAsync(model, block.MaintenanceBlockId, block.FacilityId);

        if (!ModelState.IsValid)
        {
            await PopulateFacilityOptionsAsync(model, block.FacilityId);
            return View(model);
        }

        block.FacilityId = model.FacilityId;
        block.StartAt = model.StartAt!.Value;
        block.EndAt = model.EndAt!.Value;
        block.Reason = model.Reason.Trim();
        block.IsActive = model.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The maintenance block could not be updated. Check the submitted values and try again.");

            await PopulateFacilityOptionsAsync(model, block.FacilityId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Maintenance block updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Displays the Delete confirmation page.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var block = await MaintenanceBlockQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.MaintenanceBlockId == id);

        return block is null ? NotFound() : View(block);
    }

    // Deletes the confirmed maintenance block.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var block = await _context.MaintenanceBlocks.FindAsync(id);

        if (block is null)
        {
            return NotFound();
        }

        _context.MaintenanceBlocks.Remove(block);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "The maintenance block could not be deleted. Try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Maintenance block deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Performs business validation that Data Annotations cannot cover.
    private async Task ValidateMaintenanceBlockAsync(
        MaintenanceBlockFormViewModel model,
        int? excludedBlockId = null,
        int? currentFacilityId = null)
    {
        var facilityExists = await _context.Facilities.AnyAsync(item =>
            item.FacilityId == model.FacilityId &&
            (item.IsActive || item.FacilityId == currentFacilityId));

        if (!facilityExists)
        {
            ModelState.AddModelError(nameof(model.FacilityId), "Select an available facility.");
        }

        if (!model.StartAt.HasValue || !model.EndAt.HasValue || model.EndAt <= model.StartAt)
        {
            return;
        }

        var startAt = model.StartAt.Value;
        var endAt = model.EndAt.Value;

        if (model.IsActive && endAt <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(model.EndAt), "An active maintenance block must end in the future.");
        }

        if (!model.IsActive)
        {
            return;
        }

        var overlapsAnotherBlock = await _context.MaintenanceBlocks.AnyAsync(item =>
            item.FacilityId == model.FacilityId &&
            item.IsActive &&
            (!excludedBlockId.HasValue || item.MaintenanceBlockId != excludedBlockId.Value) &&
            item.StartAt < endAt &&
            item.EndAt > startAt);

        if (overlapsAnotherBlock)
        {
            ModelState.AddModelError(
                nameof(model.StartAt),
                "This period overlaps another active maintenance block for the facility.");
        }

        if (await HasReservationConflictAsync(model.FacilityId, startAt, endAt))
        {
            ModelState.AddModelError(
                nameof(model.StartAt),
                "This period contains an existing Pending or Approved reservation.");
        }
    }

    // Checks whether a maintenance period clashes with a reservation.
    private async Task<bool> HasReservationConflictAsync(
        int facilityId,
        DateTime startAt,
        DateTime endAt)
    {
        var startDate = DateOnly.FromDateTime(startAt);
        var endDate = DateOnly.FromDateTime(endAt);

        var reservations = await _context.Reservations
            .AsNoTracking()
            .Include(item => item.TimeSlot)
            .Where(item =>
                item.FacilityId == facilityId &&
                item.ReservationDate >= startDate &&
                item.ReservationDate <= endDate &&
                (item.Status == ReservationStatuses.Pending ||
                 item.Status == ReservationStatuses.Approved))
            .ToListAsync();

        return reservations.Any(item =>
        {
            var reservationStart = item.ReservationDate.ToDateTime(item.TimeSlot.StartTime);
            var reservationEnd = item.ReservationDate.ToDateTime(item.TimeSlot.EndTime);
            return reservationStart < endAt && reservationEnd > startAt;
        });
    }

    // Creates a reusable query including related data.
    private IQueryable<MaintenanceBlock> MaintenanceBlockQuery()
    {
        return _context.MaintenanceBlocks
            .Include(item => item.Facility)
            .Include(item => item.CreatedByUser);
    }

    // Loads the facility dropdown list.
    private async Task PopulateFacilityOptionsAsync(
        MaintenanceBlockFormViewModel model,
        int? currentFacilityId = null)
    {
        model.FacilityOptions = await _context.Facilities
            .AsNoTracking()
            .Where(item => item.IsActive || item.FacilityId == currentFacilityId)
            .OrderBy(item => item.Name)
            .Select(item => new SelectListItem
            {
                Value = item.FacilityId.ToString(),
                Text = item.IsActive ? item.Name : $"{item.Name} (Inactive)"
            })
            .ToListAsync();
    }

    // Reads the logged-in administrator's UserId.
    private bool TryGetCurrentUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
