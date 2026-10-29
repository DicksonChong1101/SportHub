using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// =====================================================
// STUDENT C REPORTING MODULE - ADMIN OPERATIONS DASHBOARD
// =====================================================
[Authorize(Roles = UserRoles.Admin)]
public class ReportsController : Controller
{
    private static readonly string[] StatusOrder =
    [
        ReservationStatuses.Pending,
        ReservationStatuses.Approved,
        ReservationStatuses.Rejected,
        ReservationStatuses.Cancelled,
        ReservationStatuses.Completed
    ];

    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? fromDate, DateOnly? toDate)
    {
        var reportEnd = toDate ?? MalaysiaClock.Today;
        var reportStart = fromDate ?? reportEnd.AddDays(-29);

        if (reportStart > reportEnd)
        {
            ModelState.AddModelError(string.Empty, "From date cannot be after To date.");
            reportStart = reportEnd.AddDays(-29);
        }

        // Prevents an accidental multi-year chart from becoming unreadable.
        if (reportEnd.DayNumber - reportStart.DayNumber > 365)
        {
            ModelState.AddModelError(string.Empty, "Select a reporting period of 366 days or fewer.");
            reportStart = reportEnd.AddDays(-365);
        }

        var reservations = await _context.Reservations
            .AsNoTracking()
            .Where(item =>
                item.ReservationDate >= reportStart &&
                item.ReservationDate <= reportEnd)
            .Select(item => new
            {
                item.ReservationDate,
                item.Status,
                item.ParticipantCount,
                FacilityName = item.Facility.Name,
                PaymentStatus = item.Payment == null ? null : item.Payment.Status,
                PaymentAmount = item.Payment == null ? 0m : item.Payment.Amount
            })
            .ToListAsync();

        var validUsage = reservations.Where(item =>
            item.Status is not ReservationStatuses.Cancelled and
            not ReservationStatuses.Rejected);

        var statusCounts = reservations
            .GroupBy(item => item.Status)
            .ToDictionary(group => group.Key, group => group.Count());

        var model = new AdminDashboardViewModel
        {
            FromDate = reportStart,
            ToDate = reportEnd,
            TotalReservations = reservations.Count,
            PendingReservations = reservations.Count(item => item.Status == ReservationStatuses.Pending),
            ApprovedReservations = reservations.Count(item => item.Status == ReservationStatuses.Approved),
            CompletedReservations = reservations.Count(item => item.Status == ReservationStatuses.Completed),
            TotalParticipants = validUsage.Sum(item => item.ParticipantCount),
            PaidRevenue = reservations
                .Where(item => item.PaymentStatus == PaymentStatuses.Paid)
                .Sum(item => item.PaymentAmount),
            ActiveFacilities = await _context.Facilities.CountAsync(item => item.IsActive),
            UpcomingMaintenanceBlocks = await _context.MaintenanceBlocks.CountAsync(block =>
                block.IsActive &&
                block.EndAt >= MalaysiaClock.Now &&
                block.StartAt < MalaysiaClock.Now.AddDays(7)),
            StatusBreakdown = StatusOrder
                .Select(status => new DashboardChartItemViewModel
                {
                    Label = status,
                    Value = statusCounts.GetValueOrDefault(status)
                })
                .ToList(),
            PopularFacilities = validUsage
                .GroupBy(item => item.FacilityName)
                .Select(group => new DashboardChartItemViewModel
                {
                    Label = group.Key,
                    Value = group.Count()
                })
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Label)
                .Take(8)
                .ToList(),
            RevenueTrend = reservations
                .Where(item => item.PaymentStatus == PaymentStatuses.Paid)
                .GroupBy(item => item.ReservationDate)
                .OrderBy(group => group.Key)
                .Select(group => new DashboardRevenueItemViewModel
                {
                    Label = group.Key.ToString("dd MMM"),
                    Value = group.Sum(item => item.PaymentAmount)
                })
                .ToList()
        };

        return View(model);
    }
}
