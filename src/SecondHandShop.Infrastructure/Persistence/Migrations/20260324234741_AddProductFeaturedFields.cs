using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductFeaturedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeaturedSortOrder",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsFeatured_Status_FeaturedSortOrder_CreatedAt",
                table: "Products",
                columns: new[] { "IsFeatured", "Status", "FeaturedSortOrder", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_IsFeatured_Status_FeaturedSortOrder_CreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FeaturedSortOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Products");
        }
    }
}
