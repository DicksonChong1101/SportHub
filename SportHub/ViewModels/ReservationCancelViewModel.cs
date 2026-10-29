using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

public class ReservationCancelViewModel
{
    public int ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public string TimeSlotText { get; set; } = string.Empty;

    [Required]
    [StringLength(300, MinimumLength = 3)]
    [Display(Name = "Cancellation reason")]
    public string CancellationReason { get; set; } = string.Empty;
}
