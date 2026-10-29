using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportHub.Data;

#nullable disable

namespace SportHub.Migrations;

// Adds one-time registration email verification without changing existing accounts.
[DbContext(typeof(AppDbContext))]
[Migration("20260907170000_AddEmailAccountActivation")]
public partial class AddEmailAccountActivation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing and seeded accounts remain usable after this migration.
        migrationBuilder.AddColumn<bool>(
            name: "EmailConfirmed",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "EmailVerificationTokens",
            columns: table => new
            {
                EmailVerificationTokenId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_EmailVerificationTokens",
                    item => item.EmailVerificationTokenId);
                table.ForeignKey(
                    name: "FK_EmailVerificationTokens_Users_UserId",
                    column: item => item.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailVerificationTokens_TokenHash",
            table: "EmailVerificationTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmailVerificationTokens_UserId",
            table: "EmailVerificationTokens",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmailVerificationTokens");

        migrationBuilder.DropColumn(
            name: "EmailConfirmed",
            table: "Users");
    }
}
