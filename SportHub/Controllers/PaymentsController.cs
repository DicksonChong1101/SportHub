using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// ======================================================
// STUDENT C PAYMENT MODULE - MEMBER INVOICE AND CHECKOUT
// This is a classroom simulation and never collects a real
// card number, bank password, PIN, OTP or eWallet password.
// ======================================================
[Authorize(Roles = UserRoles.Member)]
public class PaymentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ReceiptPdfService _receiptPdfService;

    public PaymentsController(
        AppDbContext context,
        ReceiptPdfService receiptPdfService)
    {
        _context = context;
        _receiptPdfService = receiptPdfService;
    }

    // Shows the invoice before any payment details are requested.
    [HttpGet]
    public async Task<IActionResult> Invoice(int reservationId)
    {
        var reservation = await GetMemberReservationAsync(reservationId, false);
        if (reservation is null)
        {
            return NotFound();
        }

        if (!CanSubmitPayment(reservation))
        {
            TempData["ErrorMessage"] = GetPaymentEligibilityError(reservation);
            return RedirectToAction("Details", "Reservations", new { id = reservationId });
        }

        return View(BuildCheckoutViewModel(reservation));
    }

    // Displays the three supported Malaysian simulated payment choices.
    [HttpGet]
    public async Task<IActionResult> Checkout(int reservationId)
    {
        var reservation = await GetMemberReservationAsync(reservationId, false);
        if (reservation is null)
        {
            return NotFound();
        }

        if (!CanSubmitPayment(reservation))
        {
            TempData["ErrorMessage"] = GetPaymentEligibilityError(reservation);
            return RedirectToAction("Details", "Reservations", new { id = reservationId });
        }

        return View(BuildCheckoutViewModel(reservation));
    }

    // Stores a payment submission as Pending Verification for Admin approval.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(PaymentCheckoutViewModel model)
    {
        var reservation = await GetMemberReservationAsync(model.ReservationId, true);
        if (reservation is null)
        {
            return NotFound();
        }

        if (!CanSubmitPayment(reservation))
        {
            ModelState.AddModelError(string.Empty, GetPaymentEligibilityError(reservation));
        }

        if (!ModelState.IsValid)
        {
            CopyTrustedDisplayValues(model, reservation);
            PopulatePaymentMethods(model);
            return View(model);
        }

        var payment = reservation.Payment ?? new Payment
        {
            ReservationId = reservation.ReservationId,
            CreatedAt = DateTime.UtcNow
        };

        if (reservation.Payment is null)
        {
            _context.Payments.Add(payment);
        }

        // Amount and invoice number always come from trusted server data.
        payment.Amount = reservation.TotalAmount;
        payment.InvoiceNumber = BuildInvoiceNumber(reservation.ReservationNumber);
        payment.Method = model.Method;
        payment.TransactionReference = model.TransactionReference.Trim().ToUpperInvariant();
        payment.PayerAccountLastFour = model.PayerAccountLastFour;
        payment.Status = PaymentStatuses.PendingVerification;
        payment.SubmittedAt = DateTime.UtcNow;
        payment.PaidAt = null;
        payment.ReceiptNumber = null;
        payment.RefundedAt = null;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The payment could not be submitted. Refresh the page and try again.");
            CopyTrustedDisplayValues(model, reservation);
            PopulatePaymentMethods(model);
            return View(model);
        }

        TempData["SuccessMessage"] =
            "Payment details submitted successfully. Your reservation is waiting for Admin approval.";

        return RedirectToAction("Details", "Reservations", new { id = reservation.ReservationId });
    }

    // Downloads an immutable e-receipt only after Admin verifies the payment.
    [HttpGet]
    public async Task<IActionResult> Receipt(int reservationId)
    {
        var reservation = await GetMemberReservationAsync(reservationId, false);
        if (reservation is null)
        {
            return NotFound();
        }

        var canDownload = reservation.Payment?.Status == PaymentStatuses.Paid &&
            reservation.Status is ReservationStatuses.Approved or ReservationStatuses.Completed;

        if (!canDownload)
        {
            TempData["ErrorMessage"] =
                "The PDF receipt becomes available after Admin verifies the payment.";
            return RedirectToAction("Details", "Reservations", new { id = reservationId });
        }

        var pdf = _receiptPdfService.CreateReceipt(reservation);
        var fileName = $"SportHub-{reservation.Payment!.ReceiptNumber ?? reservation.ReservationNumber}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    // Downloads proof that a submitted payment was refunded after Admin rejection.
    [HttpGet]
    public async Task<IActionResult> RefundNotice(int reservationId)
    {
        var reservation = await GetMemberReservationAsync(reservationId, false);
        if (reservation is null)
        {
            return NotFound();
        }

        var canDownload = reservation.Status == ReservationStatuses.Rejected &&
            reservation.Payment?.Status == PaymentStatuses.Refunded;

        if (!canDownload)
        {
            TempData["ErrorMessage"] =
                "A PDF refund confirmation is available only for a refunded rejected reservation.";
            return RedirectToAction("Details", "Reservations", new { id = reservationId });
        }

        var pdf = _receiptPdfService.CreateRefundNotice(reservation);
        var reference = reservation.Payment!.InvoiceNumber ?? reservation.ReservationNumber;
        var fileName = $"SportHub-Refund-{reference}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    private async Task<Reservation?> GetMemberReservationAsync(int reservationId, bool tracking)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        var query = _context.Reservations
            .Include(item => item.User)
            .Include(item => item.Facility)
                .ThenInclude(facility => facility.FacilityCategory)
            .Include(item => item.TimeSlot)
            .Include(item => item.Payment)
            .Where(item => item.ReservationId == reservationId && item.UserId == userId);

        return tracking
            ? await query.SingleOrDefaultAsync()
            : await query.AsNoTracking().SingleOrDefaultAsync();
    }

    private static bool CanSubmitPayment(Reservation reservation)
    {
        var retryablePayment = reservation.Payment is null ||
            reservation.Payment.Status is PaymentStatuses.Unpaid or PaymentStatuses.Failed;

        return reservation.Status == ReservationStatuses.Pending && retryablePayment;
    }

    private static string GetPaymentEligibilityError(Reservation reservation)
    {
        if (reservation.Payment?.Status == PaymentStatuses.PendingVerification)
        {
            return "This payment has already been submitted and is waiting for Admin verification.";
        }

        if (reservation.Payment?.Status == PaymentStatuses.Paid)
        {
            return "This reservation has already been paid and approved.";
        }

        return "Only a Pending reservation without an active payment can be paid.";
    }

    private static PaymentCheckoutViewModel BuildCheckoutViewModel(Reservation reservation)
    {
        var model = new PaymentCheckoutViewModel
        {
            Method = PaymentMethods.TouchNGo
        };

        CopyTrustedDisplayValues(model, reservation);
        PopulatePaymentMethods(model);
        return model;
    }

    private static void CopyTrustedDisplayValues(
        PaymentCheckoutViewModel model,
        Reservation reservation)
    {
        model.ReservationId = reservation.ReservationId;
        model.ReservationNumber = reservation.ReservationNumber;
        model.InvoiceNumber = BuildInvoiceNumber(reservation.ReservationNumber);
        model.FacilityName = reservation.Facility.Name;
        model.ReservationDate = reservation.ReservationDate;
        model.TimeSlotText = reservation.TimeSlot.DisplayText;
        model.Amount = reservation.TotalAmount;
    }

    private static void PopulatePaymentMethods(PaymentCheckoutViewModel model)
    {
        model.MethodOptions =
        [
            new SelectListItem("Touch 'n Go eWallet", PaymentMethods.TouchNGo),
            new SelectListItem("DuitNow Transfer", PaymentMethods.DuitNow),
            new SelectListItem("Online Banking", PaymentMethods.OnlineBanking)
        ];
    }

    private static string BuildInvoiceNumber(string reservationNumber)
    {
        var suffix = reservationNumber.StartsWith("SH-", StringComparison.OrdinalIgnoreCase)
            ? reservationNumber[3..]
            : reservationNumber;

        return $"INV-{suffix}";
    }
}
