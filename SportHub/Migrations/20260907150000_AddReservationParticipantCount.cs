using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportHub.Data;

#nullable disable

namespace SportHub.Migrations;

// Stores the member's selected group size for capacity validation and reporting.
[DbContext(typeof(AppDbContext))]
[Migration("20260907150000_AddReservationParticipantCount")]
public partial class AddReservationParticipantCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ParticipantCount",
            table: "Reservations",
            type: "int",
            nullable: false,
            defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ParticipantCount",
            table: "Reservations");
    }
}
