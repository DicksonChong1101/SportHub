using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

[Index(nameof(ReservationId), IsUnique = true)]
[Index(nameof(ReceiptNumber), IsUnique = true)]
[Index(nameof(InvoiceNumber), IsUnique = true)]
public class Payment
{
    public int PaymentId { get; set; }
    public int ReservationId { get; set; }

    [Range(0.01, 9999.99)]
    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [Required, MaxLength(30)]
    [AllowedValues(PaymentMethods.TouchNGo, PaymentMethods.DuitNow,
        PaymentMethods.OnlineBanking)]
    public string Method { get; set; } = PaymentMethods.TouchNGo;

    [Required, MaxLength(20)]
    [AllowedValues(PaymentStatuses.Unpaid, PaymentStatuses.PendingVerification,
        PaymentStatuses.Paid,
        PaymentStatuses.Refunded, PaymentStatuses.Failed)]
    public string Status { get; set; } = PaymentStatuses.Unpaid;

    [MaxLength(100)]
    public string? TransactionReference { get; set; }

    // Stores only the final four characters for a safe simulated checkout.
    [StringLength(4, MinimumLength = 4)]
    public string? PayerAccountLastFour { get; set; }

    [MaxLength(30)]
    public string? InvoiceNumber { get; set; }

    [MaxLength(30)]
    public string? ReceiptNumber { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Reservation Reservation { get; set; } = null!;
}
