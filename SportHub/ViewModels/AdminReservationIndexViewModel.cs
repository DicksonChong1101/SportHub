using Microsoft.AspNetCore.Mvc.Rendering;
using SportHub.Models;

namespace SportHub.ViewModels;

// Carries the administrator's filter values,
// dropdown options and reservation results.
public class AdminReservationIndexViewModel
{
    public string? Status { get; set; }
    public int? FacilityId { get; set; }
    public DateOnly? ReservationDate { get; set; }
    public string? MemberSearch { get; set; }

    public IReadOnlyList<Reservation> Reservations { get; set; } = [];
    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> FacilityOptions { get; set; } = [];
}
