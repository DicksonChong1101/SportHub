using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportHub.Data;

#nullable disable

namespace SportHub.Migrations;

// Keeps earlier rejected test records consistent with the refund workflow.
[DbContext(typeof(AppDbContext))]
[Migration("20260908090000_BackfillRejectedPaymentRefunds")]
public partial class BackfillRejectedPaymentRefunds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE payment
            SET payment.Status = N'Refunded',
                payment.RefundedAt = COALESCE(payment.RefundedAt, SYSUTCDATETIME())
            FROM Payments AS payment
            INNER JOIN Reservations AS reservation
                ON reservation.ReservationId = payment.ReservationId
            WHERE reservation.Status = N'Rejected'
              AND payment.SubmittedAt IS NOT NULL
              AND payment.Status IN (N'Pending Verification', N'Paid', N'Failed');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Historical payment outcomes are intentionally not reversed.
    }
}
