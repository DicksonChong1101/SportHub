using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportHub.Data;

#nullable disable

namespace SportHub.Migrations;

// Adds an explicit Indoor/Outdoor classification for Home-page grouping.
[DbContext(typeof(AppDbContext))]
[Migration("20260907143000_AddFacilityType")]
public partial class AddFacilityType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FacilityType",
            table: "Facilities",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Indoor");

        // Assigns clearly outdoor existing records while safely leaving others Indoor.
        migrationBuilder.Sql(
            """
            UPDATE [Facilities]
            SET [FacilityType] = 'Outdoor'
            WHERE LOWER([Location]) LIKE '%outdoor%'
               OR LOWER([Name]) LIKE '%futsal%'
               OR LOWER([Description]) LIKE '%outdoor%';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FacilityType",
            table: "Facilities");
    }
}
