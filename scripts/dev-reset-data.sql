-- ============================================================================
--  dev-reset-data.sql
--  PURPOSE: Wipe transactional + category data from the development database
--           so the app starts from a clean slate. AdminUsers are preserved;
--           AdminSeedService re-ensures the seed admin on the next startup and
--           CatalogSeedService re-plants the default category hierarchy when
--           the Categories table is empty.
--
--  DO NOT RUN THIS AGAINST ANY NON-DEVELOPMENT DATABASE.
--  There is no undo. Everything listed in the TRUNCATE block is gone.
--
--  Tables preserved:
--    - "AdminUsers"           (admin accounts; seed is idempotent but custom
--                              accounts/password changes would be lost)
--    - "__EFMigrationsHistory" (must never be truncated, EF would re-run all
--                               migrations and likely corrupt the schema)
--
--  Tables cleared (order does not matter because CASCADE handles FKs):
--    - "ProductImages"        (FK -> Products)
--    - "ProductSales"         (FK -> Products)
--    - "ProductCategories"    (FK -> Products, FK -> Categories)
--    - "Inquiries"            (FK -> Products, FK -> Customers)
--    - "InquiryIpCooldowns"   (standalone rate-limit state)
--    - "Products"             (cover-image/image-count denorm will reset on
--                              next create flow)
--    - "Customers"            (customer rows get re-created via inquiries;
--                              there are no admin-authored customers in
--                              current flows, so this is safe to wipe)
--    - "Categories"           (cleared so CatalogSeedService re-plants the
--                              default hierarchy on next startup)
--
--  HOW TO RUN
--    Option A — psql (preferred, gives row-count confirmation per table):
--        psql "$DEV_CONNECTION_STRING" -v ON_ERROR_STOP=1 -f scripts/dev-reset-data.sql
--
--    Option B — Supabase SQL editor:
--        Copy the TRUNCATE statement below (NOT the comments) into the editor
--        and run it. The editor will display "Success" when done.
-- ============================================================================

BEGIN;

TRUNCATE TABLE
    "ProductImages",
    "ProductSales",
    "ProductCategories",
    "Inquiries",
    "InquiryIpCooldowns",
    "Products",
    "Customers",
    "Categories"
RESTART IDENTITY CASCADE;

COMMIT;

-- After this script completes, restart the WebApi once so AdminSeedService
-- re-verifies the admin user and EF's change tracker is not holding any
-- pre-reset entity references.
