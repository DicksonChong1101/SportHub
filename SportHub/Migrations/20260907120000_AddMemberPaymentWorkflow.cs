using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportHub.Data;

#nullable disable

namespace SportHub.Migrations;

// Adds only non-sensitive fields needed by the simulated member payment flow.
[DbContext(typeof(AppDbContext))]
[Migration("20260907120000_AddMemberPaymentWorkflow")]
public partial class AddMemberPaymentWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "InvoiceNumber",
            table: "Payments",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PayerAccountLastFour",
            table: "Payments",
            type: "nvarchar(4)",
            maxLength: 4,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SubmittedAt",
            table: "Payments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_InvoiceNumber",
            table: "Payments",
            column: "InvoiceNumber",
            unique: true,
            filter: "[InvoiceNumber] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payments_InvoiceNumber",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "InvoiceNumber",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "PayerAccountLastFour",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "SubmittedAt",
            table: "Payments");
    }
}
