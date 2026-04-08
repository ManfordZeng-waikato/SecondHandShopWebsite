using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecondHandShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCrmFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastContactAtUtc",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PrimarySource",
                table: "Customers",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            // --- Data backfill ---

            // 1. Set LastContactAtUtc to the latest activity date (inquiry or sale)
            migrationBuilder.Sql("""
                UPDATE "Customers" c
                SET "LastContactAtUtc" = GREATEST(
                    (SELECT MAX(i."CreatedAt") FROM "Inquiries" i WHERE i."CustomerId" = c."Id"),
                    (SELECT MAX(s."SoldAtUtc") FROM "ProductSales" s WHERE s."CustomerId" = c."Id"),
                    c."CreatedAt"
                )
                WHERE c."LastContactAtUtc" IS NULL;
                """);

            // 2. For customers who have sales but no inquiries, set PrimarySource = Sale (2)
            migrationBuilder.Sql("""
                UPDATE "Customers" c
                SET "PrimarySource" = 2
                WHERE EXISTS (SELECT 1 FROM "ProductSales" s WHERE s."CustomerId" = c."Id")
                  AND NOT EXISTS (SELECT 1 FROM "Inquiries" i WHERE i."CustomerId" = c."Id");
                """);

            // 3. Create Customer records for orphan ProductSale records with buyer contact info
            //    but no linked CustomerId. Uses email-first matching to avoid duplicates.
            migrationBuilder.Sql("""
                WITH orphan_sales AS (
                    SELECT s."Id" AS sale_id,
                           s."BuyerName",
                           LOWER(TRIM(s."BuyerEmail")) AS email,
                           TRIM(s."BuyerPhone") AS phone,
                           s."SoldAtUtc"
                    FROM "ProductSales" s
                    WHERE s."CustomerId" IS NULL
                      AND (NULLIF(TRIM(s."BuyerEmail"), '') IS NOT NULL
                           OR NULLIF(TRIM(s."BuyerPhone"), '') IS NOT NULL)
                ),
                -- Match orphans to existing customers by email first, then phone
                matched AS (
                    SELECT os.sale_id,
                           COALESCE(
                               (SELECT c."Id" FROM "Customers" c WHERE c."Email" = os.email AND os.email IS NOT NULL LIMIT 1),
                               (SELECT c."Id" FROM "Customers" c WHERE c."PhoneNumber" = os.phone AND os.phone IS NOT NULL LIMIT 1)
                           ) AS existing_customer_id,
                           os."BuyerName", os.email, os.phone, os."SoldAtUtc"
                    FROM orphan_sales os
                ),
                -- For unmatched orphans, deduplicate by email then phone to create one customer per contact
                new_customers AS (
                    SELECT DISTINCT ON (COALESCE(m.email, m.phone))
                           gen_random_uuid() AS new_id,
                           m."BuyerName" AS name,
                           m.email,
                           m.phone,
                           m."SoldAtUtc"
                    FROM matched m
                    WHERE m.existing_customer_id IS NULL
                    ORDER BY COALESCE(m.email, m.phone), m."SoldAtUtc" DESC
                ),
                inserted AS (
                    INSERT INTO "Customers" ("Id", "Name", "Email", "PhoneNumber", "Status", "PrimarySource", "Notes", "CreatedAt", "UpdatedAt", "LastContactAtUtc")
                    SELECT nc.new_id, nc.name, nc.email, nc.phone,
                           1,  -- Status = New
                           2,  -- PrimarySource = Sale
                           NULL,
                           nc."SoldAtUtc", nc."SoldAtUtc", nc."SoldAtUtc"
                    FROM new_customers nc
                    RETURNING "Id", "Email", "PhoneNumber"
                )
                -- Link orphan sales to newly created customers
                UPDATE "ProductSales" s
                SET "CustomerId" = COALESCE(
                    (SELECT i."Id" FROM inserted i WHERE i."Email" = LOWER(TRIM(s."BuyerEmail")) AND LOWER(TRIM(s."BuyerEmail")) IS NOT NULL LIMIT 1),
                    (SELECT i."Id" FROM inserted i WHERE i."PhoneNumber" = TRIM(s."BuyerPhone") AND TRIM(s."BuyerPhone") IS NOT NULL LIMIT 1)
                )
                FROM orphan_sales os
                WHERE s."Id" = os.sale_id
                  AND s."CustomerId" IS NULL
                  AND (SELECT m.existing_customer_id FROM matched m WHERE m.sale_id = s."Id") IS NULL;
                """);

            // 4. Link orphan sales that matched existing customers
            migrationBuilder.Sql("""
                UPDATE "ProductSales" s
                SET "CustomerId" = sub.customer_id
                FROM (
                    SELECT ps."Id" AS sale_id,
                           COALESCE(
                               (SELECT c."Id" FROM "Customers" c WHERE c."Email" = LOWER(TRIM(ps."BuyerEmail")) AND NULLIF(TRIM(ps."BuyerEmail"), '') IS NOT NULL LIMIT 1),
                               (SELECT c."Id" FROM "Customers" c WHERE c."PhoneNumber" = TRIM(ps."BuyerPhone") AND NULLIF(TRIM(ps."BuyerPhone"), '') IS NOT NULL LIMIT 1)
                           ) AS customer_id
                    FROM "ProductSales" ps
                    WHERE ps."CustomerId" IS NULL
                      AND (NULLIF(TRIM(ps."BuyerEmail"), '') IS NOT NULL OR NULLIF(TRIM(ps."BuyerPhone"), '') IS NOT NULL)
                ) sub
                WHERE s."Id" = sub.sale_id
                  AND sub.customer_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastContactAtUtc",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PrimarySource",
                table: "Customers");
        }
    }
}
