using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportHub.Models;

namespace SportHub.ViewModels;

// Carries the trusted reservation display values
// and the administrator's requested status change.
public class AdminReservationStatusViewModel
{
    public int ReservationId { get; set; }

    public string ReservationNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public string TimeSlotText { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;

    [Required]
    [AllowedValues(
        ReservationStatuses.Approved,
        ReservationStatuses.Rejected,
        ReservationStatuses.Completed)]
    [Display(Name = "New status")]
    public string NewStatus { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Rejection reason")]
    public string? RejectionReason { get; set; }

    // Detects whether another administrator changed this reservation first.
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    // Returns the administrator to the same filtered list after processing.
    public string? ReturnUrl { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
}
