using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryAntiSpamGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageHash",
                table: "Inquiries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestIpAddress",
                table: "Inquiries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_MessageHash_CreatedAt",
                table: "Inquiries",
                columns: new[] { "MessageHash", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_ProductId_Email_CreatedAt",
                table: "Inquiries",
                columns: new[] { "ProductId", "Email", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_ProductId_RequestIpAddress_CreatedAt",
                table: "Inquiries",
                columns: new[] { "ProductId", "RequestIpAddress", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inquiries_MessageHash_CreatedAt",
                table: "Inquiries");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_ProductId_Email_CreatedAt",
                table: "Inquiries");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_ProductId_RequestIpAddress_CreatedAt",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "MessageHash",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "RequestIpAddress",
                table: "Inquiries");
        }
    }
}
