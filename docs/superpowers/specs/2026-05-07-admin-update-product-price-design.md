# Admin Update Product Price — Design

Date: 2026-05-07
Status: Approved (brainstorming phase)

## Goal

Allow admins to modify the listed price (`Product.Price`) of an existing product
through the admin UI, without going through a full product edit flow.

## Scope decisions

1. **Allowed states:** `Available` and `OffShelf`. Editing the price of a `Sold`
   product is rejected — the historical sale price lives on
   `ProductSale.FinalSoldPrice` and must not be perturbed by changing
   `Product.Price`. To change the price of a sold product, the admin must first
   revert the sale via the existing flow.
2. **UI entry point:** A "Modify price" action in the per-row action area of
   `AdminProductsPage`, opening a dialog. No inline cell editing, no full edit
   page.
3. **Audit:** Serilog log line only. No new DB table. The existing
   `AuditableEntity` fields (`UpdatedAt` / `UpdatedByAdminUserId`) capture
   who/when at the row level.

Non-goals: bulk price update; price history UI; discount/sale-price modeling;
exposing the full `UpdateDetails` flow.

## Domain layer

`Product` gets a focused method:

```csharp
public void UpdatePrice(decimal newPrice, Guid? adminUserId, DateTime utcNow)
```

Behavior:

- Reuses the existing private `ValidatePrice(price)` (requires `> 0`).
- If `Status == ProductStatus.Sold`, throws
  `DomainRuleViolationException("Cannot modify price of a sold product. Revert the sale first.")`.
- If `newPrice == Price`, returns without mutating state (no audit touch, no
  EF change tracking dirty).
- Otherwise sets `Price = newPrice` and calls `Touch(adminUserId, utcNow)`.

Rationale: not reusing `UpdateDetails` because it requires title, slug,
description, and category — semantically a different operation. A dedicated
method keeps invariants explicit.

## Application layer

`IAdminCatalogService` gains:

```csharp
Task UpdateProductPriceAsync(
    Guid productId,
    decimal newPrice,
    Guid? adminUserId,
    CancellationToken cancellationToken);
```

`AdminCatalogService` implementation:

1. Load the product via the existing repository pattern used by
   `UpdateProductStatusAsync` / `UpdateProductFeaturedAsync`.
2. Capture `oldPrice = product.Price`.
3. Call `product.UpdatePrice(newPrice, adminUserId, _clock.UtcNow)`.
4. Persist via `SaveChanges`.
5. Emit a Serilog info log:
   ```
   Admin {AdminUserId} changed price of product {ProductId} ({Slug}) from {OldPrice} to {NewPrice}
   ```

Product-not-found and concurrency handling follow the same conventions as the
sibling admin catalog service methods.

## API layer

New endpoint on `AdminProductsController`:

```
PUT /api/lord/products/{productId:guid}/price
Authorize: AdminFullAccess
Body: { "price": <decimal> }
Response: 204 No Content
```

Errors:

- `price <= 0` or missing → 400 Bad Request via existing model validation /
  controller guard.
- Product not found → handled by existing global mapping (404).
- `DomainRuleViolationException` (Sold) → 422 Unprocessable Entity via existing
  global exception mapping (`ApiExceptionFilter`). Controller does not
  special-case.

New DTO:

```csharp
public sealed record UpdateProductPriceRequest(decimal Price);
```

## Frontend

`frontend/src/features/admin/api/` — add:

```ts
updateProductPrice(productId: string, price: number): Promise<void>
```

calling `PUT /api/lord/products/{productId}/price`.

`AdminProductsPage`:

- Add a "Modify price" entry in the same per-row action area used by status /
  featured controls.
- The entry is hidden (or disabled with a tooltip) when
  `product.status === 'Sold'`.
- Clicking opens `EditPriceDialog`:
  - Header: product title.
  - Read-only line: current price.
  - Numeric input for new price (`min={0.01}`, `step={0.01}`,
    keyboard-friendly).
  - Cancel / Save buttons.
  - On save: React Query mutation → on success
    `queryClient.invalidateQueries` for the admin product list, close dialog,
    show success snackbar.
  - Backend 4xx → display the server `ErrorResponse.message` in the dialog
    (do not close).

## Testing / verification

- Backend manual checks: price update succeeds for `Available` and `OffShelf`;
  rejected (409) for `Sold`; rejected (400) for `price <= 0`.
- Frontend: run dev server, change price on a real product, confirm list
  refreshes; confirm action is hidden/disabled for a `Sold` row.
- Type check + lint: `npm run build`, `npm run lint`, `dotnet build`.

## Out of scope / future work

- A `ProductPriceChange` history table with a UI on the product detail page.
  Deferred until there is a concrete need to surface price history; Serilog
  retains the trail in the meantime.
