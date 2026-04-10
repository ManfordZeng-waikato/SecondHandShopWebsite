using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSaleLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSales_ProductId",
                table: "ProductSales");

            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "ProductSales",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "CancellationReason",
                table: "ProductSales",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "ProductSales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "ProductSales",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSaleId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSales_ProductId_Active",
                table: "ProductSales",
                column: "ProductId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSales_ProductId_SoldAtUtc",
                table: "ProductSales",
                columns: new[] { "ProductId", "SoldAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CurrentSaleId",
                table: "Products",
                column: "CurrentSaleId");

            // Backfill Product.CurrentSaleId for products already in Sold status.
            // Prior to this migration there was a unique index on ProductSales.ProductId,
            // so there is at most one sale row per product — the subquery is safe.
            // Status column already defaults to 1 (Completed) for existing sale rows.
            migrationBuilder.Sql(@"
                UPDATE ""Products"" p
                SET ""CurrentSaleId"" = s.""Id""
                FROM ""ProductSales"" s
                WHERE s.""ProductId"" = p.""Id""
                  AND p.""Status"" = 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSales_ProductId_Active",
                table: "ProductSales");

            migrationBuilder.DropIndex(
                name: "IX_ProductSales_ProductId_SoldAtUtc",
                table: "ProductSales");

            migrationBuilder.DropIndex(
                name: "IX_Products_CurrentSaleId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CancellationNote",
                table: "ProductSales");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "ProductSales");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "ProductSales");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProductSales");

            migrationBuilder.DropColumn(
                name: "CurrentSaleId",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSales_ProductId",
                table: "ProductSales",
                column: "ProductId",
                unique: true);
        }
    }
}
