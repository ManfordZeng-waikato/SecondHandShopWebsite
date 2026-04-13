using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SecondHandShop.Infrastructure.Persistence;

#nullable disable

namespace SecondHandShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SecondHandShopDbContext))]
    [Migration("20260413122000_AddAdminSessionSecurity")]
    public partial class AddAdminSessionSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AdminUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSuccessfulLoginAtUtc",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSuccessfulLoginIp",
                table: "AdminUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "AdminUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulLoginAtUtc",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulLoginIp",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "AdminUsers");
        }
    }
}
