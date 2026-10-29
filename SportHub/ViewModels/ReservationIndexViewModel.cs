using SportHub.Models;

namespace SportHub.ViewModels;

// ======================================================
// STUDENT B - MEMBER RESERVATION SEARCH AND FILTER
// Keeps the selected filters visible after every search.
// ======================================================
public class ReservationIndexViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool UpcomingOnly { get; set; }

    public IReadOnlyList<Reservation> Reservations { get; set; } = [];
}
