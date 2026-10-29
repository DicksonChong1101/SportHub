using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// Protects every action in this controller so members cannot access /TimeSlots.
[Authorize(Roles = UserRoles.Admin)]
public class TimeSlotsController : Controller
{
    private readonly AppDbContext _context;

    public TimeSlotsController(AppDbContext context)
    {
        _context = context;
    }

    // Displays time slots in their intended booking order.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // DisplayOrder controls the primary sequence; StartTime breaks ties.
        var timeSlots = await _context.TimeSlots
            .AsNoTracking()
            .Include(slot => slot.Reservations)
            .OrderBy(slot => slot.DisplayOrder)
            .ThenBy(slot => slot.StartTime)
            .ToListAsync();

        return View(timeSlots);
    }

    // Loads one time slot and its reservation count.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var timeSlot = await _context.TimeSlots
            .AsNoTracking()
            .Include(slot => slot.Reservations)
            .SingleOrDefaultAsync(slot => slot.TimeSlotId == id);

        return timeSlot is null ? NotFound() : View(timeSlot);
    }

    // Displays an empty form for a new time slot.
    [HttpGet]
    public IActionResult Create()
    {
        return View(new TimeSlotFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimeSlotFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var startTime = model.StartTime!.Value;
        var endTime = model.EndTime!.Value;

        if (await TimeSlotOverlapsAsync(startTime, endTime))
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                "This time range overlaps an existing time slot.");

            return View(model);
        }

        var timeSlot = new TimeSlot
        {
            StartTime = startTime,
            EndTime = endTime,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive
        };

        _context.TimeSlots.Add(timeSlot);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The time slot could not be saved. Check the submitted values and try again.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Time slot created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Loads the selected time slot and prepares its existing values for editing.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var timeSlot = await _context.TimeSlots.FindAsync(id);

        if (timeSlot is null)
        {
            return NotFound();
        }

        var model = new TimeSlotFormViewModel
        {
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            DisplayOrder = timeSlot.DisplayOrder,
            IsActive = timeSlot.IsActive
        };

        return View(model);
    }

    // Validates and saves changes while excluding the current time slot from overlap checks.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TimeSlotFormViewModel model)
    {
        var timeSlot = await _context.TimeSlots.FindAsync(id);

        if (timeSlot is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var startTime = model.StartTime!.Value;
        var endTime = model.EndTime!.Value;

        if (await TimeSlotOverlapsAsync(startTime, endTime, timeSlot.TimeSlotId))
        {
            ModelState.AddModelError(
                nameof(model.StartTime),
                "This time range overlaps an existing time slot.");

            return View(model);
        }

        timeSlot.StartTime = startTime;
        timeSlot.EndTime = endTime;
        timeSlot.DisplayOrder = model.DisplayOrder;
        timeSlot.IsActive = model.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The time slot could not be updated. Check the submitted values and try again.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Time slot updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Loads the time slot and its reservations for delete confirmation.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var timeSlot = await _context.TimeSlots
            .AsNoTracking()
            .Include(slot => slot.Reservations)
            .SingleOrDefaultAsync(slot => slot.TimeSlotId == id);

        return timeSlot is null ? NotFound() : View(timeSlot);
    }

    // Deletes an unused time slot or deactivates one that already has reservations.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var timeSlot = await _context.TimeSlots
            .Include(slot => slot.Reservations)
            .SingleOrDefaultAsync(slot => slot.TimeSlotId == id);

        if (timeSlot is null)
        {
            return NotFound();
        }

        if (timeSlot.Reservations.Count > 0)
        {
            timeSlot.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "The time slot could not be deactivated. Try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "This time slot has reservations and was deactivated instead of deleted.";

            return RedirectToAction(nameof(Index));
        }

        _context.TimeSlots.Remove(timeSlot);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The time slot could not be deleted because related records still exist.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Time slot deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // Adjacent ranges are allowed, but any shared duration is treated as an overlap.
    // The optional excluded ID lets Edit ignore the time slot being changed.
    private Task<bool> TimeSlotOverlapsAsync(
        TimeOnly startTime,
        TimeOnly endTime,
        int? excludedTimeSlotId = null)
    {
        return _context.TimeSlots.AnyAsync(slot =>
            (!excludedTimeSlotId.HasValue ||
             slot.TimeSlotId != excludedTimeSlotId.Value) &&
            startTime < slot.EndTime &&
            endTime > slot.StartTime);
    }
}
