using System.Globalization;
using System.Text;
using SportHub.Models;

namespace SportHub.Services;

// =====================================================
// STUDENT C ADDITIONAL FEATURE - PDF E-RECEIPT
// Produces a self-contained one-page PDF without storing
// temporary customer files on the web server.
// =====================================================
public class ReceiptPdfService
{
    public byte[] CreateReceipt(Reservation reservation)
    {
        if (reservation.Payment is null)
        {
            throw new ArgumentException("A paid reservation is required.", nameof(reservation));
        }

        var payment = reservation.Payment;
        var content = new StringBuilder();

        // Header band and brand.
        content.AppendLine("0.08 0.49 0.28 rg 0 730 612 112 re f");
        content.AppendLine("1 1 1 rg");
        AddText(content, "F2", 28, 48, 790, "SPORTHUB");
        AddText(content, "F1", 11, 49, 770, "RESERVE  |  PLAY  |  ENJOY");
        AddText(content, "F2", 22, 400, 785, "PAYMENT RECEIPT");

        content.AppendLine("0.08 0.24 0.18 rg");
        AddText(content, "F2", 15, 48, 700, "Receipt details");
        AddLine(content, 48, 690, 564, 690);

        var rows = new (string Label, string Value)[]
        {
            ("Receipt number", payment.ReceiptNumber ?? "Not issued"),
            ("Invoice number", payment.InvoiceNumber ?? "Not issued"),
            ("Reservation", reservation.ReservationNumber),
            ("Member", reservation.User.FullName),
            ("Facility", reservation.Facility.Name),
            ("Facility type", reservation.Facility.FacilityType),
            ("Reservation date", reservation.ReservationDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)),
            ("Time", reservation.TimeSlot.DisplayText),
            ("Participants", reservation.ParticipantCount.ToString(CultureInfo.InvariantCulture)),
            ("Payment method", payment.Method),
            ("Payment reference", payment.TransactionReference ?? "Not available"),
            ("Paid at", payment.PaidAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture) ?? "Not available")
        };

        var y = 663;
        foreach (var row in rows)
        {
            AddText(content, "F2", 10, 52, y, row.Label);
            AddText(content, "F1", 10, 205, y, row.Value);
            y -= 29;
        }

        content.AppendLine("0.95 0.98 0.96 rg 44 238 524 76 re f");
        content.AppendLine("0.08 0.24 0.18 rg");
        AddText(content, "F2", 12, 58, 282, "TOTAL PAID");
        AddText(content, "F2", 24, 390, 274,
            $"RM {payment.Amount.ToString("N2", CultureInfo.InvariantCulture)}");

        AddText(content, "F2", 12, 48, 198, "Payment verified by SportHub Admin");
        AddText(content, "F1", 10, 48, 176,
            "This receipt is generated for a simulated academic payment workflow.");
        content.AppendLine("0.36 0.42 0.39 rg");
        AddText(content, "F1", 9, 48, 78,
            $"Generated {DateTime.Now:dd MMM yyyy HH:mm} | SportHub Sports Facility Reservation System");

        return BuildPdf(content.ToString());
    }

    // Creates the member's downloadable proof of refund after Admin rejection.
    public byte[] CreateRefundNotice(Reservation reservation)
    {
        if (reservation.Payment is null)
        {
            throw new ArgumentException("A refunded payment is required.", nameof(reservation));
        }

        var payment = reservation.Payment;
        var content = new StringBuilder();

        // Header band and brand.
        content.AppendLine("0.08 0.49 0.28 rg 0 730 612 112 re f");
        content.AppendLine("1 1 1 rg");
        AddText(content, "F2", 28, 48, 790, "SPORTHUB");
        AddText(content, "F1", 11, 49, 770, "RESERVE  |  PLAY  |  ENJOY");
        AddText(content, "F2", 20, 350, 785, "REFUND CONFIRMATION");

        content.AppendLine("0.08 0.24 0.18 rg");
        AddText(content, "F2", 15, 48, 700, "Refund details");
        AddLine(content, 48, 690, 564, 690);

        var refundReference = payment.InvoiceNumber is null
            ? $"REF-{reservation.ReservationNumber}"
            : $"REF-{payment.InvoiceNumber}";

        var rows = new (string Label, string Value)[]
        {
            ("Refund reference", refundReference),
            ("Invoice number", payment.InvoiceNumber ?? "Not issued"),
            ("Reservation", reservation.ReservationNumber),
            ("Member", reservation.User.FullName),
            ("Facility", reservation.Facility.Name),
            ("Reservation date", reservation.ReservationDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)),
            ("Time", reservation.TimeSlot.DisplayText),
            ("Payment method", payment.Method),
            ("Payment reference", payment.TransactionReference ?? "Not available"),
            ("Refunded at", payment.RefundedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture) ?? "Not available"),
            ("Reason", reservation.RejectionReason ?? "Reservation rejected by Admin")
        };

        var y = 663;
        foreach (var row in rows)
        {
            AddText(content, "F2", 10, 52, y, row.Label);
            AddText(content, "F1", 10, 205, y, row.Value);
            y -= 29;
        }

        content.AppendLine("0.95 0.98 0.96 rg 44 268 524 76 re f");
        content.AppendLine("0.08 0.24 0.18 rg");
        AddText(content, "F2", 12, 58, 312, "REFUND AMOUNT");
        AddText(content, "F2", 24, 390, 304,
            $"RM {payment.Amount.ToString("N2", CultureInfo.InvariantCulture)}");

        AddText(content, "F2", 12, 48, 220, "Refund recorded by SportHub Admin");
        AddText(content, "F1", 10, 48, 198,
            "This confirmation is generated for a simulated academic payment workflow.");
        content.AppendLine("0.36 0.42 0.39 rg");
        AddText(content, "F1", 9, 48, 78,
            $"Generated {DateTime.Now:dd MMM yyyy HH:mm} | SportHub Sports Facility Reservation System");

        return BuildPdf(content.ToString());
    }

    private static void AddText(
        StringBuilder content,
        string font,
        int size,
        int x,
        int y,
        string value)
    {
        content.Append("BT /")
            .Append(font).Append(' ')
            .Append(size).Append(" Tf ")
            .Append(x).Append(' ')
            .Append(y).Append(" Td (")
            .Append(EscapePdfText(value))
            .AppendLine(") Tj ET");
    }

    private static void AddLine(StringBuilder content, int x1, int y1, int x2, int y2)
    {
        content.Append("0.82 0.86 0.89 RG 1 w ")
            .Append(x1).Append(' ').Append(y1).Append(" m ")
            .Append(x2).Append(' ').Append(y2).AppendLine(" l S");
    }

    private static string EscapePdfText(string value)
    {
        var ascii = new string(value
            .Select(character => character is >= ' ' and <= '~' ? character : '?')
            .ToArray());

        return ascii
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static byte[] BuildPdf(string pageCommands)
    {
        var streamBytes = Encoding.ASCII.GetBytes(pageCommands);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] " +
            "/Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            $"<< /Length {streamBytes.Length} >>\nstream\n{pageCommands}endstream"
        };

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var crossReferenceOffset = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Length + 1}\n");
        WriteAscii(output, "0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(output, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(output,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\n" +
            $"startxref\n{crossReferenceOffset}\n%%EOF");

        return output.ToArray();
    }

    private static void WriteAscii(Stream output, string value)
    {
        output.Write(Encoding.ASCII.GetBytes(value));
    }
}
