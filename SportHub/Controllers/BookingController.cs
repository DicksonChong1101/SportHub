using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;

namespace SportHub.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly AppDbContext _context;

    public BookingController(AppDbContext context)
    {
        _context = context;
    }

    // Shows only facilities that members are currently allowed to reserve.
    [HttpGet]
    public async Task<IActionResult> Index(string? facilityType = null)
    {
        var selectedFacilityType = facilityType is FacilityTypes.Indoor or FacilityTypes.Outdoor
            ? facilityType
            : null;

        var query = _context.Facilities
            .AsNoTracking()
            .Include(facility => facility.FacilityCategory)
            .Include(facility => facility.FacilityImages)
            .Where(facility =>
                facility.IsActive &&
                facility.FacilityCategory.IsActive)
            .AsQueryable();

        if (selectedFacilityType is not null)
        {
            query = query.Where(facility => facility.FacilityType == selectedFacilityType);
        }

        var facilities = await query
            .OrderBy(facility => facility.FacilityCategory.Name)
            .ThenBy(facility => facility.Name)
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
            .SingleOrDefaultAsync(item =>
                item.FacilityId == id &&
                item.IsActive &&
                item.FacilityCategory.IsActive);

        return facility is null ? NotFound() : View(facility);
    }
}
