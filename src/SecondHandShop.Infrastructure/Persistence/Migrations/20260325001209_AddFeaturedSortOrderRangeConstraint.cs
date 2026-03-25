using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedSortOrderRangeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_FeaturedSortOrder_Range",
                table: "Products",
                sql: "[FeaturedSortOrder] IS NULL OR ([FeaturedSortOrder] >= 0 AND [FeaturedSortOrder] <= 999)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_FeaturedSortOrder_Range",
                table: "Products");
        }
    }
}
