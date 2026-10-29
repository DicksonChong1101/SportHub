using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportHub.Models;

namespace SportHub.ViewModels;

// ======================================================
// STUDENT C PAYMENT MODULE - MEMBER CHECKOUT VIEW MODEL
// Contains safe simulated payment fields only. A banking
// password must never be requested or stored by SportHub.
// ======================================================
public class PaymentCheckoutViewModel
{
    public int ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public DateOnly ReservationDate { get; set; }
    public string TimeSlotText { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    [Required]
    [AllowedValues(PaymentMethods.TouchNGo, PaymentMethods.DuitNow,
        PaymentMethods.OnlineBanking)]
    [Display(Name = "Payment method")]
    public string Method { get; set; } = PaymentMethods.TouchNGo;

    [Required]
    [RegularExpression("^[A-Za-z0-9-]{6,40}$",
        ErrorMessage = "Enter a 6-40 character payment reference using letters, numbers or hyphens.")]
    [Display(Name = "Payment reference")]
    public string TransactionReference { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{4}$",
        ErrorMessage = "Enter exactly the final four digits of the simulated account.")]
    [Display(Name = "Account last 4 digits")]
    public string PayerAccountLastFour { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> MethodOptions { get; set; } = [];
}
