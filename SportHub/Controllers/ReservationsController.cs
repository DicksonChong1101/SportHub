using System.Data;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// Only members create and manage their own reservations.
[Authorize(Roles = UserRoles.Member)]
public class ReservationsController : Controller
{
    private readonly AppDbContext _context;
    private readonly EmailSettings _emailSettings;

    public ReservationsController(
        AppDbContext context,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _emailSettings = emailSettings.Value;
    }

    // Displays only the current member's reservations and preserves all filters.
    [HttpGet]
    public async Task<IActionResult> Index(ReservationIndexViewModel model)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var query = _context.Reservations
            .AsNoTracking()
            .Include(item => item.Facility)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .Where(item => item.UserId == userId)
            .AsQueryable();

        model.Search = NormalizeOptionalText(model.Search);
        model.Status = NormalizeOptionalText(model.Status);

        if (model.Search is not null)
        {
            query = query.Where(item =>
                item.ReservationNumber.Contains(model.Search) ||
                item.Facility.Name.Contains(model.Search));
        }

        if (model.Status is not null && IsReservationStatus(model.Status))
        {
            query = query.Where(item => item.Status == model.Status);
        }
        else if (model.Status is not null)
        {
            model.Status = null;
        }

        if (model.FromDate.HasValue)
        {
            query = query.Where(item => item.ReservationDate >= model.FromDate.Value);
        }

        if (model.ToDate.HasValue)
        {
            query = query.Where(item => item.ReservationDate <= model.ToDate.Value);
        }

        if (model.UpcomingOnly)
        {
            var today = MalaysiaClock.Today;
            query = query.Where(item =>
                item.ReservationDate >= today &&
                item.Status != ReservationStatuses.Cancelled &&
                item.Status != ReservationStatuses.Rejected &&
                item.Status != ReservationStatuses.Completed);
        }

        model.Reservations = await query
            .OrderByDescending(item => item.ReservationDate)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();

        return View(model);
    }

    // Student B additional feature: returns live availability without reloading the page.
    [HttpGet]
    public async Task<IActionResult> AvailableTimeSlots(
        int facilityId,
        DateOnly? reservationDate)
    {
        if (facilityId <= 0 || !reservationDate.HasValue)
        {
            return BadRequest(new { message = "Select a facility and reservation date." });
        }

        if (!MalaysiaClock.IsInsideCurrentBookingWeek(reservationDate.Value))
        {
            return BadRequest(new
            {
                message =
                    $"Bookings are limited to {MalaysiaClock.Today:dd MMM} through " +
                    $"{MalaysiaClock.CurrentWeekEnd:dd MMM yyyy}."
            });
        }

        var facilityIsActive = await _context.Facilities
            .AsNoTracking()
            .AnyAsync(item =>
                item.FacilityId == facilityId &&
                item.IsActive &&
                item.FacilityCategory.IsActive);

        if (!facilityIsActive)
        {
            return NotFound(new { message = "The selected facility is unavailable." });
        }

        var options = await BuildAvailabilityAsync(facilityId, reservationDate.Value);

        return Json(new
        {
            date = reservationDate.Value.ToString("yyyy-MM-dd"),
            availableCount = options.Count(option => option.IsAvailable),
            timeSlots = options
        });
    }

    // Loads one reservation only when it belongs to the currently logged-in member.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(item => item.Facility)
                .ThenInclude(facility => facility.FacilityCategory)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item =>
                item.ReservationId == id &&
                item.UserId == userId);

        return reservation is null ? NotFound() : View(reservation);
    }

    // Preselects a facility when the member arrives through the Reserve this facility button.
    [HttpGet]
    public async Task<IActionResult> Create(int? facilityId = null)
    {
        var model = new ReservationCreateViewModel
        {
            FacilityId = facilityId ?? 0,
            ReservationDate = MalaysiaClock.Today
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    // Validates availability and creates a Pending reservation for the current member.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel model)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        // Reload trusted facility data instead of accepting price or status from the browser.
        var facility = await _context.Facilities
            .Include(item => item.FacilityCategory)
            .SingleOrDefaultAsync(item => item.FacilityId == model.FacilityId);

        var timeSlot = await _context.TimeSlots
            .SingleOrDefaultAsync(item => item.TimeSlotId == model.TimeSlotId);

        if (facility is null || !facility.IsActive || !facility.FacilityCategory.IsActive)
        {
            ModelState.AddModelError(nameof(model.FacilityId), "Select an active facility.");
        }

        if (timeSlot is null || !timeSlot.IsActive)
        {
            ModelState.AddModelError(nameof(model.TimeSlotId), "Select an active time slot.");
        }

        if (facility is not null && model.ParticipantCount > facility.Capacity)
        {
            ModelState.AddModelError(
                nameof(model.ParticipantCount),
                $"This facility allows a maximum of {facility.Capacity} participants.");
        }

        var reservationDate = model.ReservationDate!.Value;

        if (reservationDate < MalaysiaClock.Today)
        {
            ModelState.AddModelError(nameof(model.ReservationDate), "Reservation date cannot be in the past.");
        }
        else if (reservationDate > MalaysiaClock.CurrentWeekEnd)
        {
            ModelState.AddModelError(
                nameof(model.ReservationDate),
                $"Bookings can only be made through Sunday, {MalaysiaClock.CurrentWeekEnd:dd MMM yyyy}.");
        }

        if (!ModelState.IsValid || facility is null || timeSlot is null)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var reservationStart = reservationDate.ToDateTime(timeSlot.StartTime);
        var reservationEnd = reservationDate.ToDateTime(timeSlot.EndTime);

        if (reservationStart <= MalaysiaClock.Now)
        {
            ModelState.AddModelError(
                nameof(model.ReservationDate),
                "The selected reservation time must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        // Serializable isolation closes the small race window between checking
        // availability and saving, so two simultaneous requests cannot both win.
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        // Two date-time ranges conflict when they share any duration.
        var hasMaintenanceConflict = await _context.MaintenanceBlocks.AnyAsync(block =>
            block.FacilityId == facility.FacilityId &&
            block.IsActive &&
            block.StartAt < reservationEnd &&
            block.EndAt > reservationStart);

        if (hasMaintenanceConflict)
        {
            ModelState.AddModelError(
                nameof(model.TimeSlotId),
                "The facility is unavailable during this time because of maintenance or closure.");
        }

        // Cancelled and rejected reservations no longer occupy the facility and time slot.
        var hasReservationConflict = await _context.Reservations.AnyAsync(item =>
            item.FacilityId == facility.FacilityId &&
            item.ReservationDate == reservationDate &&
            item.TimeSlotId == timeSlot.TimeSlotId &&
            item.Status != ReservationStatuses.Cancelled &&
            item.Status != ReservationStatuses.Rejected);

        if (hasReservationConflict)
        {
            ModelState.AddModelError(
                nameof(model.TimeSlotId),
                "This facility and time slot have already been reserved for the selected date.");
        }

        if (!ModelState.IsValid)
        {
            await transaction.RollbackAsync();
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var reservation = new Reservation
        {
            // The generated value stays within the Entity's 30-character limit.
            ReservationNumber = $"SH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
            UserId = userId,
            FacilityId = facility.FacilityId,
            TimeSlotId = timeSlot.TimeSlotId,
            ReservationDate = reservationDate,
            ParticipantCount = model.ParticipantCount,
            Status = ReservationStatuses.Pending,
            // Price comes from the database, not from a browser form field.
            TotalAmount = facility.PricePerSlot,
            Notes = NormalizeOptionalText(model.Notes),
            QrToken = Guid.NewGuid().ToString("N")
        };

        _context.Reservations.Add(reservation);

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "The reservation could not be created. Refresh the page and try again.");

            await PopulateOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Reservation created successfully.";
        return RedirectToAction(nameof(Details), new { id = reservation.ReservationId });
    }

    // Student B additional feature: generates a real SVG QR pass owned by this member.
    [HttpGet]
    public async Task<IActionResult> QrCode(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item =>
                item.ReservationId == id &&
                item.UserId == userId);

        var canDisplayPass = reservation?.Payment?.Status is
            PaymentStatuses.PendingVerification or PaymentStatuses.Paid;

        if (reservation is null || !canDisplayPass ||
            string.IsNullOrWhiteSpace(reservation.QrToken))
        {
            return NotFound();
        }

        // A phone cannot open localhost on the development computer.
        // Prefer the same Visual Studio Dev Tunnel used by password-reset emails.
        var passUrl = BuildPublicPassUrl(reservation.QrToken);

        if (passUrl is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "The reservation pass URL could not be generated.");
        }

        using var qrCodeData = QRCodeGenerator.GenerateQrCode(
            passUrl,
            QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new SvgQRCode(qrCodeData);
        var svg = qrCode.GetGraphic(6);

        // Prevents the browser from reusing an older localhost QR image.
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml");
    }

    // The scanner can validate a pass without exposing the member's private profile.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Pass(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NotFound();
        }

        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(item => item.Facility)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.QrToken == token);

        var canValidatePass = reservation?.Payment?.Status is
            PaymentStatuses.PendingVerification or PaymentStatuses.Paid;

        return reservation is null || !canValidatePass ? NotFound() : View(reservation);
    }

    // Student B additional feature: exports a booking to any calendar application.
    [HttpGet]
    public async Task<IActionResult> Calendar(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(item => item.Facility)
            .Include(item => item.TimeSlot)
            .SingleOrDefaultAsync(item =>
                item.ReservationId == id &&
                item.UserId == userId);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status is ReservationStatuses.Cancelled or ReservationStatuses.Rejected)
        {
            return BadRequest("Cancelled or rejected reservations cannot be added to a calendar.");
        }

        var start = reservation.ReservationDate.ToDateTime(reservation.TimeSlot.StartTime);
        var end = reservation.ReservationDate.ToDateTime(reservation.TimeSlot.EndTime);
        var calendar = BuildCalendarFile(reservation, start, end);

        return File(
            Encoding.UTF8.GetBytes(calendar),
            "text/calendar; charset=utf-8",
            $"{reservation.ReservationNumber}.ics");
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(item => item.Facility)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item =>
                item.ReservationId == id &&
                item.UserId == userId);

        if (reservation is null)
        {
            return NotFound();
        }

        if (!CanCancel(reservation))
        {
            TempData["ErrorMessage"] =
                GetCancellationEligibilityError(reservation);

            return RedirectToAction(nameof(Details), new { id });
        }

        return View(BuildCancelViewModel(reservation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(ReservationCancelViewModel model)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Challenge();
        }

        var reservation = await _context.Reservations
            .Include(item => item.Facility)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item =>
                item.ReservationId == model.ReservationId &&
                item.UserId == userId);

        if (reservation is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            CopyCancelDisplayValues(model, reservation);
            return View(model);
        }

        if (!CanCancel(reservation))
        {
            TempData["ErrorMessage"] =
                GetCancellationEligibilityError(reservation);

            return RedirectToAction(nameof(Details), new { id = reservation.ReservationId });
        }

        reservation.Status = ReservationStatuses.Cancelled;
        reservation.CancellationReason = model.CancellationReason.Trim();
        reservation.CancelledAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The reservation could not be cancelled. Refresh the page and try again.");

            CopyCancelDisplayValues(model, reservation);
            return View(model);
        }

        TempData["SuccessMessage"] = "Reservation cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id = reservation.ReservationId });
    }

    private async Task PopulateOptionsAsync(ReservationCreateViewModel model)
    {
        // The browser receives the same limits that the server enforces.
        model.MinimumReservationDate = MalaysiaClock.Today;
        model.MaximumReservationDate = MalaysiaClock.CurrentWeekEnd;

        model.FacilityOptions = await _context.Facilities
            .AsNoTracking()
            .Where(item => item.IsActive && item.FacilityCategory.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new SelectListItem
            {
                Value = item.FacilityId.ToString(),
                Text = $"{item.Name} - RM {item.PricePerSlot:N2}"
            })
            .ToListAsync();

        model.FacilityCapacities = await _context.Facilities
            .AsNoTracking()
            .Where(item => item.IsActive && item.FacilityCategory.IsActive)
            .ToDictionaryAsync(item => item.FacilityId, item => item.Capacity);

        model.TimeSlotOptions = await _context.TimeSlots
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.StartTime)
            .Select(item => new SelectListItem
            {
                Value = item.TimeSlotId.ToString(),
                Text = item.DisplayText
            })
            .ToListAsync();
    }

    // Calculates the same availability rules used by the final server-side POST.
    private async Task<IReadOnlyList<AvailabilityOption>> BuildAvailabilityAsync(
        int facilityId,
        DateOnly reservationDate)
    {
        var timeSlots = await _context.TimeSlots
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.StartTime)
            .ToListAsync();

        var occupiedTimeSlotIds = await _context.Reservations
            .AsNoTracking()
            .Where(item =>
                item.FacilityId == facilityId &&
                item.ReservationDate == reservationDate &&
                item.Status != ReservationStatuses.Cancelled &&
                item.Status != ReservationStatuses.Rejected)
            .Select(item => item.TimeSlotId)
            .ToListAsync();

        var dayStart = reservationDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var maintenanceBlocks = await _context.MaintenanceBlocks
            .AsNoTracking()
            .Where(block =>
                block.FacilityId == facilityId &&
                block.IsActive &&
                block.StartAt < dayEnd &&
                block.EndAt > dayStart)
            .ToListAsync();

        return timeSlots.Select(timeSlot =>
        {
            var start = reservationDate.ToDateTime(timeSlot.StartTime);
            var end = reservationDate.ToDateTime(timeSlot.EndTime);
            var isPast = start <= MalaysiaClock.Now;
            var isReserved = occupiedTimeSlotIds.Contains(timeSlot.TimeSlotId);
            var isMaintenance = maintenanceBlocks.Any(block =>
                block.StartAt < end && block.EndAt > start);

            var reason = isPast
                ? "Past time"
                : isReserved
                    ? "Already reserved"
                    : isMaintenance
                        ? "Maintenance or closure"
                        : null;

            return new AvailabilityOption(
                timeSlot.TimeSlotId,
                timeSlot.DisplayText,
                reason is null,
                reason);
        }).ToList();
    }

    private static bool IsReservationStatus(string status)
    {
        return status is ReservationStatuses.Pending
            or ReservationStatuses.Approved
            or ReservationStatuses.Rejected
            or ReservationStatuses.Cancelled
            or ReservationStatuses.Completed;
    }

    private static string BuildCalendarFile(
        Reservation reservation,
        DateTime start,
        DateTime end)
    {
        static string Escape(string value) => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        var uid = $"{reservation.QrToken ?? reservation.ReservationNumber}@sporthub.local";

        return string.Join("\r\n",
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//SportHub//Reservation Calendar//EN",
            "CALSCALE:GREGORIAN",
            "BEGIN:VEVENT",
            $"UID:{Escape(uid)}",
            $"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}",
            $"DTSTART:{start:yyyyMMdd'T'HHmmss}",
            $"DTEND:{end:yyyyMMdd'T'HHmmss}",
            $"SUMMARY:{Escape($"SportHub - {reservation.Facility.Name}")}",
            $"LOCATION:{Escape(reservation.Facility.Location)}",
            $"DESCRIPTION:{Escape($"Reservation {reservation.ReservationNumber} ({reservation.Status})")}",
            "END:VEVENT",
            "END:VCALENDAR",
            string.Empty);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Builds a phone-accessible QR destination when a Dev Tunnel is active.
    private string? BuildPublicPassUrl(string token)
    {
        var relativeUrl = Url.Action(
            nameof(Pass),
            "Reservations",
            new { token });

        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return null;
        }

        var externalBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL");

        if (string.IsNullOrWhiteSpace(externalBaseUrl))
        {
            externalBaseUrl = _emailSettings.PublicBaseUrl;
        }

        if (Uri.TryCreate(externalBaseUrl, UriKind.Absolute, out var baseUri) &&
            (baseUri.Scheme == Uri.UriSchemeHttps ||
             baseUri.Scheme == Uri.UriSchemeHttp))
        {
            return new Uri(baseUri, relativeUrl.TrimStart('/')).ToString();
        }

        // Localhost remains useful when scanning is tested on the same computer.
        return Url.Action(
            nameof(Pass),
            "Reservations",
            new { token },
            Request.Scheme);
    }

    private static bool CanCancel(Reservation reservation)
    {
        var reservationStart = reservation.ReservationDate
            .ToDateTime(reservation.TimeSlot.StartTime);

        return reservationStart > MalaysiaClock.Now &&
               reservation.Status is ReservationStatuses.Pending or ReservationStatuses.Approved &&
               reservation.Payment?.Status is not
                   (PaymentStatuses.PendingVerification or PaymentStatuses.Paid);
    }

    // Explains whether cancellation is blocked
    // by payment or by reservation status/time.
    private static string GetCancellationEligibilityError(Reservation reservation)
    {
        return reservation.Payment?.Status is
            PaymentStatuses.PendingVerification or PaymentStatuses.Paid
            ? "A submitted payment must be reviewed by an administrator before this reservation can be changed."
            : "Only future Pending or Approved reservations can be cancelled.";
    }

    private static ReservationCancelViewModel BuildCancelViewModel(Reservation reservation)
    {
        return new ReservationCancelViewModel
        {
            ReservationId = reservation.ReservationId,
            ReservationNumber = reservation.ReservationNumber,
            FacilityName = reservation.Facility.Name,
            ReservationDate = reservation.ReservationDate,
            TimeSlotText = reservation.TimeSlot.DisplayText
        };
    }

    private static void CopyCancelDisplayValues(
        ReservationCancelViewModel model,
        Reservation reservation)
    {
        model.ReservationNumber = reservation.ReservationNumber;
        model.FacilityName = reservation.Facility.Name;
        model.ReservationDate = reservation.ReservationDate;
        model.TimeSlotText = reservation.TimeSlot.DisplayText;
    }

    private sealed record AvailabilityOption(
        int TimeSlotId,
        string Text,
        bool IsAvailable,
        string? Reason);
}
