using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// Only administrators can access reservation management.
[Authorize(Roles = UserRoles.Admin)]
public class AdminReservationsController : Controller
{
    private static readonly string[] AllStatuses =
    [
        ReservationStatuses.Pending,
        ReservationStatuses.Approved,
        ReservationStatuses.Rejected,
        ReservationStatuses.Cancelled,
        ReservationStatuses.Completed
    ];

    private readonly AppDbContext _context;

    public AdminReservationsController(AppDbContext context)
    {
        _context = context;
    }

    // Gives administrators a searchable overview of every member reservation.
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        int? facilityId,
        DateOnly? reservationDate,
        string? memberSearch)
    {
        var query = _context.Reservations
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Facility)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && AllStatuses.Contains(status))
        {
            query = query.Where(item => item.Status == status);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(item => item.FacilityId == facilityId.Value);
        }

        if (reservationDate.HasValue)
        {
            query = query.Where(item => item.ReservationDate == reservationDate.Value);
        }

        var normalizedSearch = memberSearch?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(item =>
                item.User.FullName.Contains(normalizedSearch) ||
                item.User.Email.Contains(normalizedSearch) ||
                item.ReservationNumber.Contains(normalizedSearch));
        }

        var model = new AdminReservationIndexViewModel
        {
            Status = AllStatuses.Contains(status) ? status : null,
            FacilityId = facilityId,
            ReservationDate = reservationDate,
            MemberSearch = normalizedSearch,
            Reservations = await query
                .OrderByDescending(item => item.ReservationDate)
                .ThenByDescending(item => item.CreatedAt)
                .ToListAsync()
        };

        await PopulateFilterOptionsAsync(model);
        return View(model);
    }

    // Loads one reservation together with all
    // information needed by the administrator.
    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl = null)
    {
        var reservation = await ReservationDetailsQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ReservationId == id);

        if (reservation is null)
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = GetSafeReturnUrl(returnUrl);
        return View(reservation);
    }

    // Displays only the legal next statuses
    // for the reservation's current status.
    [HttpGet]
    public async Task<IActionResult> UpdateStatus(int id, string? returnUrl = null)
    {
        var reservation = await ReservationDetailsQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ReservationId == id);

        if (reservation is null)
        {
            return NotFound();
        }

        var allowedStatuses = GetAllowedNextStatuses(reservation);
        if (allowedStatuses.Count == 0)
        {
            TempData["ErrorMessage"] =
                $"A {reservation.Status} reservation is final and cannot be changed.";

            return RedirectToAction(
                nameof(Details),
                new { id, returnUrl = GetSafeReturnUrl(returnUrl) });
        }

        var model = BuildStatusViewModel(reservation, allowedStatuses);
        model.ReturnUrl = GetSafeReturnUrl(returnUrl);
        return View(model);
    }

    // Rechecks every status rule on the server
    // before saving the administrator's selection.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        AdminReservationStatusViewModel model)
    {
        if (id != model.ReservationId)
        {
            return BadRequest();
        }

        var reservation = await ReservationDetailsQuery()
            .SingleOrDefaultAsync(item => item.ReservationId == id);

        if (reservation is null)
        {
            return NotFound();
        }

        var allowedStatuses = GetAllowedNextStatuses(reservation);
        if (!allowedStatuses.Contains(model.NewStatus))
        {
            ModelState.AddModelError(
                nameof(model.NewStatus),
                $"A {reservation.Status} reservation cannot be changed to {model.NewStatus}.");
        }

        if (model.NewStatus == ReservationStatuses.Rejected &&
            string.IsNullOrWhiteSpace(model.RejectionReason))
        {
            ModelState.AddModelError(
                nameof(model.RejectionReason),
                "A rejection reason is required when rejecting a reservation.");
        }

        byte[] submittedRowVersion = [];
        try
        {
            submittedRowVersion = Convert.FromBase64String(model.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "The reservation version is invalid. Refresh and try again.");
        }

        if (!ModelState.IsValid)
        {
            CopyStatusDisplayValues(model, reservation, allowedStatuses);
            return View(model);
        }

        // Uses the submitted RowVersion to detect
        // whether another administrator saved first.
        _context.Entry(reservation)
            .Property(item => item.RowVersion)
            .OriginalValue = submittedRowVersion;

        reservation.Status = model.NewStatus;
        reservation.UpdatedAt = DateTime.UtcNow;

        if (model.NewStatus == ReservationStatuses.Approved)
        {
            reservation.ApprovedAt = DateTime.UtcNow;
            reservation.RejectionReason = null;

            // Admin approval verifies the submitted simulated payment.
            if (reservation.Payment is not null)
            {
                reservation.Payment.Status = PaymentStatuses.Paid;
                reservation.Payment.PaidAt = DateTime.UtcNow;
                reservation.Payment.ReceiptNumber ??= GenerateReceiptNumber();
            }
        }
        else if (model.NewStatus == ReservationStatuses.Rejected)
        {
            reservation.RejectionReason = model.RejectionReason!.Trim();

            // A submitted simulated payment is returned when Admin rejects
            // the reservation. The refund timestamp supports the member PDF.
            if (reservation.Payment?.Status is
                PaymentStatuses.PendingVerification or PaymentStatuses.Paid)
            {
                reservation.Payment.Status = PaymentStatuses.Refunded;
                reservation.Payment.RefundedAt = DateTime.UtcNow;
            }
        }
        else if (model.NewStatus == ReservationStatuses.Completed)
        {
            reservation.CompletedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await _context.Entry(reservation).ReloadAsync();

            ModelState.AddModelError(
                string.Empty,
                "Another administrator updated this reservation. Review the latest status and try again.");

            allowedStatuses = GetAllowedNextStatuses(reservation);
            CopyStatusDisplayValues(model, reservation, allowedStatuses);
            return View(model);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The reservation status could not be updated. Refresh and try again.");

            CopyStatusDisplayValues(model, reservation, allowedStatuses);
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Reservation {reservation.ReservationNumber} is now {reservation.Status}.";

        return RedirectToAction(
            nameof(Details),
            new { id, returnUrl = GetSafeReturnUrl(model.ReturnUrl) });
    }

    // Creates one reusable query for the
    // administrator's reservation detail pages.
    private IQueryable<Reservation> ReservationDetailsQuery()
    {
        return _context.Reservations
            .Include(item => item.User)
            .Include(item => item.Facility)
                .ThenInclude(item => item.FacilityCategory)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment);
    }

    // Prepares the status and facility dropdown
    // options used by the filter form.
    private async Task PopulateFilterOptionsAsync(AdminReservationIndexViewModel model)
    {
        model.StatusOptions =
        [
            new SelectListItem("All statuses", string.Empty),
            .. AllStatuses.Select(item => new SelectListItem(item, item))
        ];

        var facilities = await _context.Facilities
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        model.FacilityOptions =
        [
            new SelectListItem("All facilities", string.Empty),
            .. facilities.Select(item => new SelectListItem(
                item.IsActive ? item.Name : $"{item.Name} (Inactive)",
                item.FacilityId.ToString()))
        ];
    }

    // Defines the only legal reservation
    // status transitions in the system.
    private static IReadOnlyList<string> GetAllowedNextStatuses(Reservation reservation)
    {
        return reservation.Status switch
        {
            ReservationStatuses.Pending
                when reservation.Payment?.Status == PaymentStatuses.PendingVerification =>
                [ReservationStatuses.Approved, ReservationStatuses.Rejected],
            ReservationStatuses.Pending =>
                [ReservationStatuses.Rejected],
            ReservationStatuses.Approved =>
                [ReservationStatuses.Completed],
            _ => []
        };
    }

    // The receipt is issued only after Admin verifies and approves payment.
    private static string GenerateReceiptNumber()
    {
        return $"RCPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22]
            .ToUpperInvariant();
    }

    // Accepts only a local return address so an external URL cannot be injected.
    private string GetSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(Index))!;
    }

    private static AdminReservationStatusViewModel BuildStatusViewModel(
        Reservation reservation,
        IReadOnlyList<string> allowedStatuses)
    {
        var model = new AdminReservationStatusViewModel();
        CopyStatusDisplayValues(model, reservation, allowedStatuses);
        return model;
    }

    // Rebuilds trusted display values and legal choices after every invalid submission.
    private static void CopyStatusDisplayValues(
        AdminReservationStatusViewModel model,
        Reservation reservation,
        IReadOnlyList<string> allowedStatuses)
    {
        model.ReservationId = reservation.ReservationId;
        model.ReservationNumber = reservation.ReservationNumber;
        model.MemberName = reservation.User.FullName;
        model.FacilityName = reservation.Facility.Name;
        model.ReservationDate = reservation.ReservationDate;
        model.TimeSlotText = reservation.TimeSlot.DisplayText;
        model.CurrentStatus = reservation.Status;
        model.RowVersion = Convert.ToBase64String(reservation.RowVersion);
        model.StatusOptions = allowedStatuses
            .Select(item => new SelectListItem(item, item))
            .ToList();
    }
}
