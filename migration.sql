CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "AdminUsers" (
        "Id" uuid NOT NULL,
        "DisplayName" character varying(120) NOT NULL,
        "Email" character varying(256) NOT NULL,
        "UserName" character varying(120) NOT NULL DEFAULT '',
        "PasswordHash" character varying(512) NOT NULL DEFAULT '',
        "Role" character varying(50) NOT NULL DEFAULT '',
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_AdminUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "Customers" (
        "Id" uuid NOT NULL,
        "Name" character varying(120),
        "Email" character varying(256),
        "PhoneNumber" character varying(40),
        "Status" smallint NOT NULL DEFAULT 1,
        "Notes" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Customers" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Customers_AtLeastOneContact" CHECK ((NULLIF(TRIM("Email"), '') IS NOT NULL) OR (NULLIF(TRIM("PhoneNumber"), '') IS NOT NULL))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "InquiryIpCooldowns" (
        "IpAddress" character varying(64) NOT NULL,
        "BlockedUntil" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_InquiryIpCooldowns" PRIMARY KEY ("IpAddress")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "Categories" (
        "Id" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "Slug" character varying(160) NOT NULL,
        "ParentCategoryId" uuid,
        "SortOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedByAdminUserId" uuid,
        "UpdatedByAdminUserId" uuid,
        CONSTRAINT "PK_Categories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Categories_AdminUsers_CreatedByAdminUserId" FOREIGN KEY ("CreatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Categories_AdminUsers_UpdatedByAdminUserId" FOREIGN KEY ("UpdatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Categories_Categories_ParentCategoryId" FOREIGN KEY ("ParentCategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "Products" (
        "Id" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Slug" character varying(220) NOT NULL,
        "Description" character varying(4000) NOT NULL,
        "Price" numeric(18,2) NOT NULL,
        "Condition" smallint,
        "Status" smallint NOT NULL DEFAULT 1,
        "CategoryId" uuid NOT NULL,
        "SoldAt" timestamp with time zone,
        "OffShelvedAt" timestamp with time zone,
        "IsFeatured" boolean NOT NULL DEFAULT FALSE,
        "FeaturedSortOrder" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedByAdminUserId" uuid,
        "UpdatedByAdminUserId" uuid,
        CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Products_FeaturedSortOrder_Range" CHECK ("FeaturedSortOrder" IS NULL OR ("FeaturedSortOrder" >= 0 AND "FeaturedSortOrder" <= 999)),
        CONSTRAINT "CK_Products_Price" CHECK ("Price" > 0),
        CONSTRAINT "FK_Products_AdminUsers_CreatedByAdminUserId" FOREIGN KEY ("CreatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Products_AdminUsers_UpdatedByAdminUserId" FOREIGN KEY ("UpdatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Products_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "Inquiries" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "CustomerName" character varying(120),
        "Email" character varying(256),
        "PhoneNumber" character varying(40),
        "RequestIpAddress" character varying(64),
        "MessageHash" character varying(64) NOT NULL,
        "Message" character varying(3000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "EmailDeliveryStatus" smallint NOT NULL DEFAULT 1,
        "DeliveredAt" timestamp with time zone,
        "DeliveryError" character varying(1000),
        "EmailSendAttempts" integer NOT NULL DEFAULT 0,
        "NextRetryAt" timestamp with time zone,
        CONSTRAINT "PK_Inquiries" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Inquiries_AtLeastOneContact" CHECK ((NULLIF(TRIM("Email"), '') IS NOT NULL) OR (NULLIF(TRIM("PhoneNumber"), '') IS NOT NULL)),
        CONSTRAINT "FK_Inquiries_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Inquiries_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE TABLE "ProductImages" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "CloudStorageKey" character varying(500) NOT NULL,
        "Url" character varying(1024) NOT NULL DEFAULT '',
        "AltText" character varying(300),
        "SortOrder" integer NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedByAdminUserId" uuid,
        "UpdatedByAdminUserId" uuid,
        CONSTRAINT "PK_ProductImages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProductImages_AdminUsers_CreatedByAdminUserId" FOREIGN KEY ("CreatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_ProductImages_AdminUsers_UpdatedByAdminUserId" FOREIGN KEY ("UpdatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ProductImages_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_AdminUsers_Email" ON "AdminUsers" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_AdminUsers_UserName" ON "AdminUsers" ("UserName") WHERE "UserName" <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Categories_CreatedByAdminUserId" ON "Categories" ("CreatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Categories_ParentCategoryId_SortOrder" ON "Categories" ("ParentCategoryId", "SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_Categories_Slug" ON "Categories" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Categories_UpdatedByAdminUserId" ON "Categories" ("UpdatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_Customers_Email" ON "Customers" ("Email") WHERE "Email" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_Customers_PhoneNumber" ON "Customers" ("PhoneNumber") WHERE "PhoneNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Inquiries_CustomerId_CreatedAt" ON "Inquiries" ("CustomerId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Inquiries_MessageHash_CreatedAt" ON "Inquiries" ("MessageHash", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Inquiries_ProductId_CreatedAt" ON "Inquiries" ("ProductId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Inquiries_ProductId_Email_CreatedAt" ON "Inquiries" ("ProductId", "Email", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Inquiries_ProductId_RequestIpAddress_CreatedAt" ON "Inquiries" ("ProductId", "RequestIpAddress", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_InquiryIpCooldowns_BlockedUntil" ON "InquiryIpCooldowns" ("BlockedUntil");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_ProductImages_CreatedByAdminUserId" ON "ProductImages" ("CreatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_ProductImages_ProductId" ON "ProductImages" ("ProductId") WHERE "IsPrimary" = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_ProductImages_ProductId_SortOrder" ON "ProductImages" ("ProductId", "SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_ProductImages_UpdatedByAdminUserId" ON "ProductImages" ("UpdatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Products_CategoryId_Status" ON "Products" ("CategoryId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Products_CreatedByAdminUserId" ON "Products" ("CreatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Products_IsFeatured_Status_FeaturedSortOrder_CreatedAt" ON "Products" ("IsFeatured", "Status", "FeaturedSortOrder", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE UNIQUE INDEX "IX_Products_Slug" ON "Products" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Products_Status_UpdatedAt" ON "Products" ("Status", "UpdatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Products_Title" ON "Products" ("Title");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    CREATE INDEX "IX_Products_UpdatedByAdminUserId" ON "Products" ("UpdatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331032553_InitialPostgresCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260331032553_InitialPostgresCreate', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331104500_AddAdminMustChangePassword') THEN
    ALTER TABLE "AdminUsers" ADD "MustChangePassword" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331104500_AddAdminMustChangePassword') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260331104500_AddAdminMustChangePassword', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE TABLE "ProductSales" (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "CustomerId" uuid,
        "InquiryId" uuid,
        "ListedPriceAtSale" numeric(18,2) NOT NULL,
        "FinalSoldPrice" numeric(18,2) NOT NULL,
        "BuyerName" character varying(200),
        "BuyerPhone" character varying(40),
        "BuyerEmail" character varying(256),
        "SoldAtUtc" timestamp with time zone NOT NULL,
        "PaymentMethod" smallint,
        "Notes" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedByAdminUserId" uuid,
        "UpdatedByAdminUserId" uuid,
        CONSTRAINT "PK_ProductSales" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_ProductSales_FinalSoldPrice" CHECK ("FinalSoldPrice" >= 0),
        CONSTRAINT "FK_ProductSales_AdminUsers_CreatedByAdminUserId" FOREIGN KEY ("CreatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_ProductSales_AdminUsers_UpdatedByAdminUserId" FOREIGN KEY ("UpdatedByAdminUserId") REFERENCES "AdminUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ProductSales_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_ProductSales_Inquiries_InquiryId" FOREIGN KEY ("InquiryId") REFERENCES "Inquiries" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_ProductSales_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE INDEX "IX_ProductSales_CreatedByAdminUserId" ON "ProductSales" ("CreatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE INDEX "IX_ProductSales_CustomerId" ON "ProductSales" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE INDEX "IX_ProductSales_InquiryId" ON "ProductSales" ("InquiryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE UNIQUE INDEX "IX_ProductSales_ProductId" ON "ProductSales" ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE INDEX "IX_ProductSales_SoldAtUtc" ON "ProductSales" ("SoldAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    CREATE INDEX "IX_ProductSales_UpdatedByAdminUserId" ON "ProductSales" ("UpdatedByAdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331230205_AddProductSaleTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260331230205_AddProductSaleTable', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
    ALTER TABLE "Customers" ADD "LastContactAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
    ALTER TABLE "Customers" ADD "PrimarySource" smallint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
    UPDATE "Customers" c
    SET "LastContactAtUtc" = GREATEST(
        (SELECT MAX(i."CreatedAt") FROM "Inquiries" i WHERE i."CustomerId" = c."Id"),
        (SELECT MAX(s."SoldAtUtc") FROM "ProductSales" s WHERE s."CustomerId" = c."Id"),
        c."CreatedAt"
    )
    WHERE c."LastContactAtUtc" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
    UPDATE "Customers" c
    SET "PrimarySource" = 2
    WHERE EXISTS (SELECT 1 FROM "ProductSales" s WHERE s."CustomerId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "Inquiries" i WHERE i."CustomerId" = c."Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260408031159_AddCustomerCrmFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260408031159_AddCustomerCrmFields', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260409024902_AddProductImageDenormalizationColumns') THEN
    ALTER TABLE "Products" ADD "CoverImageKey" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260409024902_AddProductImageDenormalizationColumns') THEN
    ALTER TABLE "Products" ADD "ImageCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260409024902_AddProductImageDenormalizationColumns') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260409024902_AddProductImageDenormalizationColumns') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260409024902_AddProductImageDenormalizationColumns', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    DROP INDEX "IX_ProductSales_ProductId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    ALTER TABLE "ProductSales" ADD "CancellationNote" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    ALTER TABLE "ProductSales" ADD "CancellationReason" smallint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    ALTER TABLE "ProductSales" ADD "CancelledAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    ALTER TABLE "ProductSales" ADD "Status" smallint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    ALTER TABLE "Products" ADD "CurrentSaleId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    CREATE UNIQUE INDEX "IX_ProductSales_ProductId_Active" ON "ProductSales" ("ProductId") WHERE "Status" = 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    CREATE INDEX "IX_ProductSales_ProductId_SoldAtUtc" ON "ProductSales" ("ProductId", "SoldAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    CREATE INDEX "IX_Products_CurrentSaleId" ON "Products" ("CurrentSaleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN

                    UPDATE "Products" p
                    SET "CurrentSaleId" = s."Id"
                    FROM "ProductSales" s
                    WHERE s."ProductId" = p."Id"
                      AND p."Status" = 2;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410035113_AddProductSaleLifecycle') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260410035113_AddProductSaleLifecycle', '10.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260410040604_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260410040604_InitialCreate', '10.0.4');
    END IF;
END $EF$;
COMMIT;

