using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// ======================================================
// STUDENT A CORE - ADMIN USER MANAGEMENT
// Search, inspect, update, activate and unlock accounts.
// ======================================================
[Authorize(Roles = UserRoles.Admin)]
public class AdminUsersController : Controller
{
    private readonly AppDbContext _context;

    public AdminUsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? role,
        bool? isActive)
    {
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                user.FullName.Contains(term) ||
                user.Email.Contains(term));
        }

        if (role is UserRoles.Admin or UserRoles.Member)
        {
            query = query.Where(user => user.Role == role);
        }
        else
        {
            role = null;
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        var users = await query
            .OrderBy(user => user.FullName)
            .Select(user => new AdminUserListItemViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LockoutEnd = user.LockoutEnd,
                LastLoginAt = user.LastLoginAt,
                ReservationCount = user.Reservations.Count
            })
            .ToListAsync();

        return View(new AdminUserIndexViewModel
        {
            Search = search,
            Role = role,
            IsActive = isActive,
            Users = users
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(account => account.Reservations)
            .Include(account => account.LoginAttempts)
            .SingleOrDefaultAsync(account => account.UserId == id);

        return user is null ? NotFound() : View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.UserId == id);

        if (user is null)
        {
            return NotFound();
        }

        return View(new AdminUserEditViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AdminUserEditViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest();
        }

        if (model.Role is not (UserRoles.Admin or UserRoles.Member))
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Select a valid account role.");
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(account => account.UserId == id);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();

        if (currentUserId == user.UserId &&
            (!model.IsActive || model.Role != UserRoles.Admin))
        {
            ModelState.AddModelError(
                string.Empty,
                "You cannot deactivate or remove the Admin role from your own signed-in account.");
        }

        if (!ModelState.IsValid)
        {
            model.Email = user.Email;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
            ? null
            : model.PhoneNumber.Trim();
        user.Role = model.Role;
        user.IsActive = model.IsActive;

        if (!user.IsActive)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "User account updated successfully.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(account => account.UserId == id);

        if (user is null)
        {
            return NotFound();
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The account lockout has been cleared.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId)
            ? userId
            : null;
    }
}
