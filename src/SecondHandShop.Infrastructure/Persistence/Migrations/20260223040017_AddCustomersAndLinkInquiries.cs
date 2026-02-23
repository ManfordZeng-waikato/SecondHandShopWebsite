using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersAndLinkInquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AdminUsers_UpdatedByAdminUserId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductImages_AdminUsers_UpdatedByAdminUserId",
                table: "ProductImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AdminUsers_UpdatedByAdminUserId",
                table: "Products");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Inquiries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_AtLeastOneContact", "(NULLIF(LTRIM(RTRIM([Email])), '') IS NOT NULL) OR (NULLIF(LTRIM(RTRIM([PhoneNumber])), '') IS NOT NULL)");
                });

            migrationBuilder.Sql("""
                INSERT INTO [Customers] ([Id], [Name], [Email], [PhoneNumber], [CreatedAt], [UpdatedAt])
                SELECT
                    [Id],
                    [CustomerName],
                    [Email],
                    [PhoneNumber],
                    [CreatedAt],
                    [CreatedAt]
                FROM [Inquiries];
                """);

            migrationBuilder.Sql("""
                UPDATE [Inquiries]
                SET [CustomerId] = [Id]
                WHERE [CustomerId] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Inquiries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_CustomerId_CreatedAt",
                table: "Inquiries",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AdminUsers_UpdatedByAdminUserId",
                table: "Categories",
                column: "UpdatedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_Customers_CustomerId",
                table: "Inquiries",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductImages_AdminUsers_UpdatedByAdminUserId",
                table: "ProductImages",
                column: "UpdatedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AdminUsers_UpdatedByAdminUserId",
                table: "Products",
                column: "UpdatedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AdminUsers_UpdatedByAdminUserId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_Customers_CustomerId",
                table: "Inquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductImages_AdminUsers_UpdatedByAdminUserId",
                table: "ProductImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AdminUsers_UpdatedByAdminUserId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_CustomerId_CreatedAt",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Inquiries");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AdminUsers_UpdatedByAdminUserId",
                table: "Categories",
                column: "UpdatedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductImages_AdminUsers_UpdatedByAdminUserId",
                table: "ProductImages",
                column: "UpdatedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AdminUsers_UpdatedByAdminUserId",
                table: "Products",
                column: "UpdatedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
