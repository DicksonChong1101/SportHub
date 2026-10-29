using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

// Prevents the same start-time and end-time combination from being duplicated.
[Index(nameof(StartTime), nameof(EndTime), IsUnique = true)]
public class TimeSlot
{
    public int TimeSlotId { get; set; }
    // TimeOnly represents a reusable daily time slot without a calendar date.
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    // Inactive time slots remain available for historical reservation records.
    public bool IsActive { get; set; } = true;

    // Controls the order in which time slots are displayed on the website.
    [Range(0, 100)]
    public int DisplayOrder { get; set; }

    // Calculated display text used by the UI; it is not stored in the database.
    [NotMapped]
    public string DisplayText => $"{StartTime:HH:mm} - {EndTime:HH:mm}";

    public ICollection<Reservation> Reservations { get; set; } = [];
}
