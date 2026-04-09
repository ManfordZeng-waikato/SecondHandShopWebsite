using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageDenormalizationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageKey",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageCount",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH cover AS (
                    SELECT DISTINCT ON ("ProductId")
                        "ProductId",
                        "CloudStorageKey" AS cover_key
                    FROM "ProductImages"
                    ORDER BY "ProductId", "IsPrimary" DESC, "SortOrder"
                ),
                counts AS (
                    SELECT "ProductId", COUNT(*)::int AS cnt
                    FROM "ProductImages"
                    GROUP BY "ProductId"
                )
                UPDATE "Products" p
                SET
                    "CoverImageKey" = cover.cover_key,
                    "ImageCount" = counts.cnt
                FROM cover
                INNER JOIN counts ON counts."ProductId" = cover."ProductId"
                WHERE p."Id" = cover."ProductId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageKey",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageCount",
                table: "Products");
        }
    }
}
