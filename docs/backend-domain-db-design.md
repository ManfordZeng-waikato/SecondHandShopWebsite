# Backend Domain and Database Design

## Layers (Clean Architecture)

- `SecondHandShop.Domain`: entities and domain rules.
- `SecondHandShop.Application`: use-case contracts, repository/messaging/time abstractions.
- `SecondHandShop.Infrastructure`: EF Core persistence, repository implementations, migration scripts.
- `SecondHandShop.WebApi`: HTTP composition root.

Dependency direction:

- `WebApi -> Application -> Domain`
- `Infrastructure` depends on `Application` and `Domain` for interface implementation only.

## Domain Entities

### `AdminUser`

- Purpose: audit owner of admin changes.
- Fields: `Id`, `DisplayName`, `Email`, `IsActive`.

### `Category`

- Multi-level category tree.
- Fields: `Id`, `Name`, `Slug`, `ParentCategoryId`, `SortOrder`, `IsActive`.
- Audit: `CreatedAt`, `UpdatedAt`, `CreatedByAdminUserId`, `UpdatedByAdminUserId`.

### `Product`

- Aggregate root for listing and status lifecycle.
- Fields: `Id`, `Title`, `Slug`, `Description`, `Price`, `Condition`, `Status`, `CategoryId`.
- Status timestamps: `SoldAt`, `OffShelvedAt`.
- Audit: `CreatedAt`, `UpdatedAt`, `CreatedByAdminUserId`, `UpdatedByAdminUserId`.
- Rule: `Price > 0`.

### `ProductImage`

- Cloud image metadata only.
- Fields: `Id`, `ProductId`, `CloudStorageKey`, `Url`, `AltText`, `SortOrder`, `IsPrimary`.
- Audit: `CreatedAt`, `UpdatedAt`, `CreatedByAdminUserId`, `UpdatedByAdminUserId`.
- Rule: per product, at most one primary image.

### `Inquiry`

- User inquiry linked to a specific product.
- Fields: `Id`, `ProductId`, `CustomerName`, `Email`, `PhoneNumber`, `Message`, `CreatedAt`.
- Delivery tracking: `EmailDeliveryStatus`, `DeliveredAt`, `DeliveryError`, `EmailSendAttempts`, `NextRetryAt`.
- Rule: `Message` is required; `Email` or `PhoneNumber` at least one is required.

## SQL Server Schema and Constraints

Tables:

- `AdminUsers`
- `Categories`
- `Products`
- `ProductImages`
- `Inquiries`

Important constraints and indexes:

- `CK_Products_Price`: `[Price] > 0`
- `CK_Inquiries_AtLeastOneContact`: at least one of `Email`/`PhoneNumber` is non-empty
- Unique: `Products.Slug`, `Categories.Slug`, `AdminUsers.Email`
- Filtered unique index: `IX_ProductImages_ProductId` with `WHERE IsPrimary = 1`
- List indexes:
  - `Products(Status, UpdatedAt)`
  - `Products(CategoryId, Status)`
  - `ProductImages(ProductId, SortOrder)`
  - `Inquiries(ProductId, CreatedAt)`
  - `Categories(ParentCategoryId, SortOrder)`

Foreign keys:

- `Products.CategoryId -> Categories.Id` (`Restrict`)
- `Categories.ParentCategoryId -> Categories.Id` (`Restrict`)
- `ProductImages.ProductId -> Products.Id` (`Cascade`)
- `Inquiries.ProductId -> Products.Id` (`Restrict`)
- Audit FKs (`CreatedByAdminUserId`, `UpdatedByAdminUserId`) -> `AdminUsers.Id` (`SetNull`)

## Query Visibility Rules

- Public product listing:
  - include only `Product.Status IN (Available, Sold)`
  - category must be active
  - exclude `OffShelf`
- Admin listing:
  - all statuses visible, including `OffShelf`

## Inquiry and Email Reliability

- Inquiry is stored first, then sent asynchronously (current placeholder sender).
- Delivery status fields support lightweight retry.
- `EmailSendAttempts` + `NextRetryAt` are prepared for worker-based retries.
- Optional future upgrade: dedicated Outbox table and worker process.

## Generated Assets

- EF migration: `src/SecondHandShop.Infrastructure/Persistence/Migrations/*_InitialCreate.cs`
- SQL script: `src/SecondHandShop.Infrastructure/Persistence/Migrations/InitialCreate.sql`
