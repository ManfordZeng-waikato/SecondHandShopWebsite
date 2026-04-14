using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductSales_Status_SoldAtUtc",
                table: "ProductSales",
                columns: new[] { "Status", "SoldAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_CreatedAt",
                table: "Inquiries",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSales_Status_SoldAtUtc",
                table: "ProductSales");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_CreatedAt",
                table: "Inquiries");
        }
    }
}
