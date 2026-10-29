using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SportHub.ViewModels;

public class ReservationCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a facility.")]
    [Display(Name = "Facility")]
    public int FacilityId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Reservation date")]
    public DateOnly? ReservationDate { get; set; }

    // Display-only boundaries calculated from Malaysia's current week.
    public DateOnly MinimumReservationDate { get; set; }
    public DateOnly MaximumReservationDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a time slot.")]
    [Display(Name = "Time slot")]
    public int TimeSlotId { get; set; }

    [Range(1, 1000, ErrorMessage = "Enter at least one participant.")]
    [Display(Name = "Number of participants")]
    public int ParticipantCount { get; set; } = 1;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Supplies active facilities to the dropdown.
    public IReadOnlyList<SelectListItem> FacilityOptions { get; set; } = [];

    // Trusted capacities used only to improve the browser experience; POST validates again.
    public IReadOnlyDictionary<int, int> FacilityCapacities { get; set; } =
        new Dictionary<int, int>();

    // Supplies active time slots to the dropdown.
    public IReadOnlyList<SelectListItem> TimeSlotOptions { get; set; } = [];
}
