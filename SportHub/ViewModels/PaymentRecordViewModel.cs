using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportHub.Models;

namespace SportHub.ViewModels;

// Separates the administrator's payment form from the Payment database entity.
public class PaymentRecordViewModel
{
    public int ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    [Required]
    [AllowedValues(
        PaymentMethods.Cash,
        PaymentMethods.Card,
        PaymentMethods.OnlineTransfer)]
    [Display(Name = "Payment method")]
    public string Method { get; set; } = PaymentMethods.Cash;

    [StringLength(100)]
    [Display(Name = "Transaction reference")]
    public string? TransactionReference { get; set; }

    // Returns to the same reservation details
    // page after recording or cancelling payment.
    public string? ReturnUrl { get; set; }
    public IReadOnlyList<SelectListItem> MethodOptions { get; set; } = [];
}
