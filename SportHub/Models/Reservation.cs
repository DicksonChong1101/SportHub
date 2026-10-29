using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

[Index(nameof(ReservationNumber), IsUnique = true)]
[Index(nameof(QrToken), IsUnique = true)]
[Index(nameof(FacilityId), nameof(ReservationDate), nameof(TimeSlotId))]
public class Reservation
{
    public int ReservationId { get; set; }

    [Required, MaxLength(30)]
    public string ReservationNumber { get; set; } = string.Empty;

    // A reservation answers three questions: who booked, which facility,
    // and which time slot. These three properties are the corresponding FKs.
    public int UserId { get; set; }
    public int FacilityId { get; set; }
    public int TimeSlotId { get; set; }

    // Specifies the calendar date selected by the user.
    public DateOnly ReservationDate { get; set; }

    // Stores the actual group size validated against the selected facility capacity.
    [Range(1, 1000)]
    public int ParticipantCount { get; set; } = 1;

    [Required, MaxLength(20)]
    [AllowedValues(ReservationStatuses.Pending, ReservationStatuses.Approved,
        ReservationStatuses.Rejected, ReservationStatuses.Cancelled,
        ReservationStatuses.Completed)]
    public string Status { get; set; } = ReservationStatuses.Pending;

    [Range(0.01, 9999.99)]
    [Precision(10, 2)]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(300)]
    public string? RejectionReason { get; set; }

    [MaxLength(300)]
    public string? CancellationReason { get; set; }

    [MaxLength(100)]
    public string? QrToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Facility Facility { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public TimeSlot TimeSlot { get; set; } = null!;

    public Payment? Payment { get; set; }
}
