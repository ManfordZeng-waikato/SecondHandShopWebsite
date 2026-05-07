# Admin Update Product Price Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let admins change `Product.Price` from the admin product list, with a Sold-state guard and a Serilog audit trail.

**Architecture:** New `Product.UpdatePrice` domain method enforces invariants (`> 0`, not Sold, no-op if unchanged). `IAdminCatalogService.UpdateProductPriceAsync` orchestrates load → mutate → save → log. New `PUT /api/lord/products/{id}/price` controller action. Frontend gets an `EditPriceDialog` reachable from a per-row "Modify price" button, hidden when status is Sold.

**Tech Stack:** .NET 10 (clean architecture, MediatR-adjacent service style), EF Core, xUnit + Moq + FluentAssertions for backend tests; React 19 + MUI 7 + TanStack Query 5 + Vitest + RTL for frontend.

**Spec:** `docs/superpowers/specs/2026-05-07-admin-update-product-price-design.md`

---

## File Structure

**Backend — modify:**
- `src/SecondHandShop.Domain/Entities/Product.cs` — add `UpdatePrice` method
- `src/SecondHandShop.Application/UseCases/Catalog/IAdminCatalogService.cs` — add `UpdateProductPriceAsync` signature
- `src/SecondHandShop.Application/UseCases/Catalog/AdminCatalogService.cs` — implement, inject `ILogger<AdminCatalogService>`
- `src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs` — add `PUT /price` endpoint + `UpdateProductPriceRequest` DTO
- `tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/AdminCatalogServiceTests.cs` — add tests for new behavior

**Frontend — modify:**
- `frontend/src/features/admin/api/adminApi.ts` — add `updateProductPrice` function
- `frontend/src/pages/AdminProductsPage.tsx` — add row action button + wire dialog + mutation

**Frontend — create:**
- `frontend/src/features/admin/components/EditPriceDialog.tsx` — dialog component

---

## Task 1: Add `Product.UpdatePrice` domain method

**Files:**
- Modify: `src/SecondHandShop.Domain/Entities/Product.cs` (insert new method right after `UpdateMainCategory`, around line 132)

- [ ] **Step 1: Add the method**

In `Product.cs`, insert this method after `UpdateMainCategory` (just before `MarkAsSold`):

```csharp
public void UpdatePrice(decimal newPrice, Guid? updatedByAdminUserId, DateTime utcNow)
{
    if (Status == ProductStatus.Sold)
    {
        throw new DomainRuleViolationException(
            "Cannot modify price of a sold product. Revert the sale first.");
    }

    ValidatePrice(newPrice);

    if (Price == newPrice)
    {
        return;
    }

    Price = newPrice;
    Touch(updatedByAdminUserId, utcNow);
}
```

Note: `ValidatePrice` and `Touch` are already defined in the file (private static / inherited from `AuditableEntity`).

- [ ] **Step 2: Build to verify it compiles**

```bash
dotnet build src/SecondHandShop.Domain/SecondHandShop.Domain.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/SecondHandShop.Domain/Entities/Product.cs
git commit -m "feat(product): add Product.UpdatePrice domain method"
```

---

## Task 2: Domain tests for `UpdatePrice`

**Files:**
- Test: `tests/SecondHandShop.Domain.UnitTests/Entities/ProductTests.cs` (modify if exists, otherwise create)

- [ ] **Step 1: Locate or create the test file**

Run:
```bash
ls tests/SecondHandShop.Domain.UnitTests/Entities/ProductTests.cs 2>/dev/null && echo EXISTS || echo MISSING
```

If it exists, add the tests below to it. If `MISSING`, create the file with this top:

```csharp
using FluentAssertions;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class ProductTests
{
    private static readonly DateTime UtcNow = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);

    private static Product CreateAvailable()
    {
        return Product.Create(
            "Vintage leather bag",
            "vintage-leather-bag",
            "Soft grain leather.",
            220m,
            Guid.NewGuid(),
            Guid.NewGuid(),
            UtcNow,
            ProductCondition.Good);
    }
}
```

- [ ] **Step 2: Add the four tests**

Add these methods inside `ProductTests`:

```csharp
[Fact]
public void UpdatePrice_ShouldSetNewPriceAndTouchAudit_WhenAvailable()
{
    var product = CreateAvailable();
    var admin = Guid.NewGuid();
    var later = UtcNow.AddMinutes(5);

    product.UpdatePrice(259m, admin, later);

    product.Price.Should().Be(259m);
    product.UpdatedAt.Should().Be(later);
    product.UpdatedByAdminUserId.Should().Be(admin);
}

[Fact]
public void UpdatePrice_ShouldAllowUpdate_WhenOffShelf()
{
    var product = CreateAvailable();
    product.OffShelf(Guid.NewGuid(), UtcNow.AddMinutes(1));

    product.UpdatePrice(199m, Guid.NewGuid(), UtcNow.AddMinutes(2));

    product.Price.Should().Be(199m);
}

[Fact]
public void UpdatePrice_ShouldThrow_WhenSold()
{
    var product = CreateAvailable();
    _ = product.MarkAsSold(200m, UtcNow.AddMinutes(1), Guid.NewGuid(), UtcNow.AddMinutes(1));

    var act = () => product.UpdatePrice(180m, Guid.NewGuid(), UtcNow.AddMinutes(2));

    act.Should().Throw<DomainRuleViolationException>()
        .WithMessage("Cannot modify price of a sold product. Revert the sale first.");
}

[Fact]
public void UpdatePrice_ShouldThrow_WhenNonPositive()
{
    var product = CreateAvailable();

    var act = () => product.UpdatePrice(0m, Guid.NewGuid(), UtcNow.AddMinutes(1));

    act.Should().Throw<ArgumentOutOfRangeException>();
}

[Fact]
public void UpdatePrice_ShouldBeNoop_WhenPriceUnchanged()
{
    var product = CreateAvailable();
    var originalUpdatedAt = product.UpdatedAt;

    product.UpdatePrice(product.Price, Guid.NewGuid(), UtcNow.AddMinutes(10));

    product.UpdatedAt.Should().Be(originalUpdatedAt);
}
```

- [ ] **Step 3: Run the tests**

```bash
dotnet test tests/SecondHandShop.Domain.UnitTests/SecondHandShop.Domain.UnitTests.csproj --filter "FullyQualifiedName~ProductTests.UpdatePrice"
```

Expected: 5 passing tests. If the test project does not exist (i.e. you had to create the file in step 1 and there's no `.csproj`), skip this task — domain coverage will then be exercised through Task 4's service tests, which already invoke `UpdatePrice`. Note this in the commit message and proceed.

- [ ] **Step 4: Commit**

```bash
git add tests/SecondHandShop.Domain.UnitTests/Entities/ProductTests.cs
git commit -m "test(product): cover Product.UpdatePrice invariants"
```

---

## Task 3: Add `UpdateProductPriceAsync` to `IAdminCatalogService`

**Files:**
- Modify: `src/SecondHandShop.Application/UseCases/Catalog/IAdminCatalogService.cs`

- [ ] **Step 1: Add the interface method**

Add this method to the `IAdminCatalogService` interface, after `UpdateProductFeaturedAsync`:

```csharp
Task UpdateProductPriceAsync(
    Guid productId,
    decimal newPrice,
    Guid? adminUserId,
    CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Build (will fail in `AdminCatalogService` until Task 4)**

```bash
dotnet build src/SecondHandShop.Application/SecondHandShop.Application.csproj
```

Expected: compile error in `AdminCatalogService` saying it does not implement `UpdateProductPriceAsync`. That is intentional — Task 4 fixes it. Do NOT commit yet; commit happens in Task 4 together with the implementation so we don't leave a broken build on the branch.

---

## Task 4: Implement `UpdateProductPriceAsync` in `AdminCatalogService`

**Files:**
- Modify: `src/SecondHandShop.Application/UseCases/Catalog/AdminCatalogService.cs`

- [ ] **Step 1: Add `Microsoft.Extensions.Logging` using and logger param**

Add at the top of the file with other usings:

```csharp
using Microsoft.Extensions.Logging;
```

Change the class primary constructor from:

```csharp
public class AdminCatalogService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductImageRepository productImageRepository,
    IObjectStorageService objectStorageService,
    IUnitOfWork unitOfWork,
    IClock clock) : IAdminCatalogService
```

to:

```csharp
public class AdminCatalogService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductImageRepository productImageRepository,
    IObjectStorageService objectStorageService,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AdminCatalogService> logger) : IAdminCatalogService
```

- [ ] **Step 2: Add the method implementation**

Insert this method after `UpdateProductFeaturedAsync` (around line 118):

```csharp
public async Task UpdateProductPriceAsync(
    Guid productId,
    decimal newPrice,
    Guid? adminUserId,
    CancellationToken cancellationToken = default)
{
    var product = await productRepository.GetByIdAsync(productId, cancellationToken)
        ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");

    var oldPrice = product.Price;
    product.UpdatePrice(newPrice, adminUserId, clock.UtcNow);

    if (oldPrice == newPrice)
    {
        return;
    }

    await unitOfWork.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
        "Admin {AdminUserId} changed price of product {ProductId} ({Slug}) from {OldPrice} to {NewPrice}.",
        adminUserId,
        product.Id,
        product.Slug,
        oldPrice,
        newPrice);
}
```

Rationale: the early `return` mirrors `UpdatePrice`'s no-op behavior — no DB write, no log.

- [ ] **Step 3: Update existing test SUT factory**

In `tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/AdminCatalogServiceTests.cs`, the `CreateSut` helper currently has 5 mock parameters. Update it to inject a logger:

Add at the top:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
```

Change the `return new AdminCatalogService(...)` call inside `CreateSut` from:

```csharp
return new AdminCatalogService(
    productRepository ?? Mock.Of<IProductRepository>(),
    categoryRepository ?? Mock.Of<ICategoryRepository>(),
    productImageRepository ?? Mock.Of<IProductImageRepository>(),
    objectStorageService ?? Mock.Of<IObjectStorageService>(),
    unitOfWork ?? Mock.Of<IUnitOfWork>(),
    new FakeClock(UtcNow));
```

to:

```csharp
return new AdminCatalogService(
    productRepository ?? Mock.Of<IProductRepository>(),
    categoryRepository ?? Mock.Of<ICategoryRepository>(),
    productImageRepository ?? Mock.Of<IProductImageRepository>(),
    objectStorageService ?? Mock.Of<IObjectStorageService>(),
    unitOfWork ?? Mock.Of<IUnitOfWork>(),
    new FakeClock(UtcNow),
    NullLogger<AdminCatalogService>.Instance);
```

- [ ] **Step 4: Build and run existing tests**

```bash
dotnet build SecondHandShopWebsite.slnx
dotnet test tests/SecondHandShop.Application.UnitTests/SecondHandShop.Application.UnitTests.csproj
```

Expected: build succeeds; all existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add src/SecondHandShop.Application/UseCases/Catalog/IAdminCatalogService.cs \
        src/SecondHandShop.Application/UseCases/Catalog/AdminCatalogService.cs \
        tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/AdminCatalogServiceTests.cs
git commit -m "feat(product): add UpdateProductPriceAsync to admin catalog service"
```

---

## Task 5: Service-level tests for `UpdateProductPriceAsync`

**Files:**
- Modify: `tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/AdminCatalogServiceTests.cs`

- [ ] **Step 1: Add three tests**

Insert these `[Fact]` methods inside `AdminCatalogServiceTests`, before the `CreateSut` helper:

```csharp
[Fact]
public async Task UpdateProductPriceAsync_ShouldUpdatePriceAndPersist()
{
    var product = CreateProduct();

    var productRepository = new Mock<IProductRepository>();
    productRepository
        .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(product);

    var unitOfWork = new Mock<IUnitOfWork>();
    unitOfWork
        .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var sut = CreateSut(
        productRepository: productRepository.Object,
        unitOfWork: unitOfWork.Object);

    await sut.UpdateProductPriceAsync(product.Id, 275m, Guid.NewGuid());

    product.Price.Should().Be(275m);
    unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task UpdateProductPriceAsync_ShouldThrow_WhenProductMissing()
{
    var productRepository = new Mock<IProductRepository>();
    productRepository
        .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Product?)null);

    var sut = CreateSut(productRepository: productRepository.Object);

    var act = () => sut.UpdateProductPriceAsync(Guid.NewGuid(), 100m, Guid.NewGuid());

    await act.Should().ThrowAsync<KeyNotFoundException>();
}

[Fact]
public async Task UpdateProductPriceAsync_ShouldNotPersist_WhenPriceUnchanged()
{
    var product = CreateProduct();

    var productRepository = new Mock<IProductRepository>();
    productRepository
        .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(product);

    var unitOfWork = new Mock<IUnitOfWork>();

    var sut = CreateSut(
        productRepository: productRepository.Object,
        unitOfWork: unitOfWork.Object);

    await sut.UpdateProductPriceAsync(product.Id, product.Price, Guid.NewGuid());

    unitOfWork.Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Never);
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test tests/SecondHandShop.Application.UnitTests/SecondHandShop.Application.UnitTests.csproj --filter "FullyQualifiedName~UpdateProductPriceAsync"
```

Expected: 3 passing tests.

- [ ] **Step 3: Commit**

```bash
git add tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/AdminCatalogServiceTests.cs
git commit -m "test(product): cover UpdateProductPriceAsync persistence and missing-product paths"
```

---

## Task 6: Add `PUT /api/lord/products/{id}/price` endpoint

**Files:**
- Modify: `src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs`

- [ ] **Step 1: Add the action and DTO**

In `AdminProductsController.cs`, add this action right after `UpdateStatusAsync` (around line 98):

```csharp
[HttpPut("{productId:guid}/price")]
public async Task<IActionResult> UpdatePriceAsync(
    Guid productId,
    [FromBody] UpdateProductPriceRequest request,
    CancellationToken cancellationToken)
{
    if (request.Price <= 0m)
    {
        return BadRequest(new ErrorResponse("Price must be greater than zero."));
    }

    var adminUserId = GetAdminUserId();
    await adminCatalogService.UpdateProductPriceAsync(productId, request.Price, adminUserId, cancellationToken);
    return NoContent();
}
```

Add the DTO at the bottom of the file, alongside the other request records (e.g. after `UpdateProductStatusRequest`):

```csharp
public sealed record UpdateProductPriceRequest(decimal Price);
```

The action does NOT special-case `DomainRuleViolationException` — the existing global exception mapping converts it to 409 Conflict. (Confirmed by sibling endpoints like `UpdateStatusAsync` which similarly forward to the service without a try/catch.)

- [ ] **Step 2: Build the WebApi project**

```bash
dotnet build src/SecondHandShop.WebApi/SecondHandShop.WebApi.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Manual smoke test**

Start the API:
```bash
dotnet run --project src/SecondHandShop.WebApi
```

In a separate terminal, log in as admin (or use an existing browser session cookie), then:
```bash
# Replace {ID} with a real product id and {COOKIE} with the shs.admin.token cookie value
curl -k -X PUT "https://localhost:7266/api/lord/products/{ID}/price" \
  -H "Content-Type: application/json" \
  -H "Cookie: shs.admin.token={COOKIE}" \
  -d '{"price": 199.99}' -i
```

Expected: `HTTP/1.1 204 No Content`.

Test the rejection path: pick a Sold product id and re-run. Expected: `409 Conflict` with body containing `"Cannot modify price of a sold product..."`.

Test invalid price: send `"price": 0`. Expected: `400 Bad Request`.

Stop the dev server.

- [ ] **Step 4: Commit**

```bash
git add src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs
git commit -m "feat(api): add PUT /api/lord/products/{id}/price endpoint"
```

---

## Task 7: Frontend API helper

**Files:**
- Modify: `frontend/src/features/admin/api/adminApi.ts`

- [ ] **Step 1: Add the helper**

Insert next to the existing `updateProductStatus` function (around line 167):

```ts
export async function updateProductPrice(productId: string, price: number): Promise<void> {
  await httpClient.put(`/api/lord/products/${productId}/price`, { price });
}
```

- [ ] **Step 2: Type-check the frontend**

```bash
cd frontend && npx tsc --noEmit && cd ..
```

Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/admin/api/adminApi.ts
git commit -m "feat(frontend): add updateProductPrice API helper"
```

---

## Task 8: `EditPriceDialog` component

**Files:**
- Create: `frontend/src/features/admin/components/EditPriceDialog.tsx`

- [ ] **Step 1: Create the file**

```tsx
import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateProductPrice } from '../api/adminApi';

interface EditPriceDialogProps {
  open: boolean;
  productId: string | null;
  productTitle: string;
  currentPrice: number;
  onClose: () => void;
  onSaved: () => void;
}

function resolveErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const maybeResponse = (error as { response?: { data?: { message?: unknown } } }).response;
    const message = maybeResponse?.data?.message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }
  }
  return fallback;
}

export function EditPriceDialog({
  open,
  productId,
  productTitle,
  currentPrice,
  onClose,
  onSaved,
}: EditPriceDialogProps) {
  const queryClient = useQueryClient();
  const [priceInput, setPriceInput] = useState<string>(String(currentPrice));
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setPriceInput(String(currentPrice));
      setErrorMessage(null);
    }
  }, [open, currentPrice]);

  const mutation = useMutation({
    mutationFn: ({ id, price }: { id: string; price: number }) => updateProductPrice(id, price),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      onSaved();
    },
    onError: (error) => {
      setErrorMessage(resolveErrorMessage(error, 'Failed to update price.'));
    },
  });

  const handleSave = () => {
    if (!productId) return;
    setErrorMessage(null);

    const trimmed = priceInput.trim();
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setErrorMessage('Price must be a positive number.');
      return;
    }

    mutation.mutate({ id: productId, price: parsed });
  };

  return (
    <Dialog open={open} onClose={mutation.isPending ? undefined : onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Modify price</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {productTitle}
          </Typography>
          <Typography variant="body2">
            Current price: <strong>${currentPrice}</strong>
          </Typography>
          <TextField
            autoFocus
            label="New price"
            type="number"
            value={priceInput}
            onChange={(event) => setPriceInput(event.target.value)}
            inputProps={{ min: 0.01, step: 0.01, inputMode: 'decimal' }}
            disabled={mutation.isPending}
            fullWidth
          />
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button variant="contained" onClick={handleSave} disabled={mutation.isPending}>
          {mutation.isPending ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
```

- [ ] **Step 2: Type-check**

```bash
cd frontend && npx tsc --noEmit && cd ..
```

Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/admin/components/EditPriceDialog.tsx
git commit -m "feat(frontend): add EditPriceDialog for admin price changes"
```

---

## Task 9: Wire the dialog into `AdminProductsPage`

**Files:**
- Modify: `frontend/src/pages/AdminProductsPage.tsx`

- [ ] **Step 1: Import the dialog**

Add to the imports near the top (next to the other dialog imports around lines 41–44):

```tsx
import { EditPriceDialog } from '../features/admin/components/EditPriceDialog';
```

- [ ] **Step 2: Add dialog state**

Inside the `AdminProductsPage` component, near the other dialog target state declarations (around line 166), add:

```tsx
const [priceTarget, setPriceTarget] = useState<AdminProductListItem | null>(null);
```

- [ ] **Step 3: Add a "Modify price" button in the controls row**

In the controls row (around line 868, right after the `History` button), insert:

```tsx
{product.status !== 'Sold' && (
  <Button
    size="small"
    variant="outlined"
    onClick={() => setPriceTarget(product)}
    sx={{ minWidth: 130, alignSelf: { xs: 'stretch', sm: 'center' } }}
  >
    Modify price
  </Button>
)}
```

- [ ] **Step 4: Render the dialog**

Near the other dialogs at the bottom of the returned JSX (around line 1026, after `<ProductCategoryDialog ... />`), add:

```tsx
<EditPriceDialog
  open={Boolean(priceTarget)}
  productId={priceTarget?.id ?? null}
  productTitle={priceTarget?.title ?? ''}
  currentPrice={priceTarget?.price ?? 0}
  onClose={() => setPriceTarget(null)}
  onSaved={() => {
    setPriceTarget(null);
    setFeedback({ severity: 'success', message: 'Price updated.' });
  }}
/>
```

- [ ] **Step 5: Type-check and lint**

```bash
cd frontend && npm run lint && npx tsc --noEmit && cd ..
```

Expected: no lint errors, no type errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/AdminProductsPage.tsx
git commit -m "feat(frontend): wire EditPriceDialog into admin products page"
```

---

## Task 10: End-to-end manual verification

**Files:** none (manual)

- [ ] **Step 1: Run backend**

```bash
dotnet run --project src/SecondHandShop.WebApi
```

- [ ] **Step 2: Run frontend dev server**

In another terminal:
```bash
cd frontend && npm run dev
```

- [ ] **Step 3: Verify happy path**

1. Open `https://localhost:5173/lord/login` and log in.
2. Navigate to the admin products page.
3. Pick an `Available` product. Click **Modify price**, enter a new price, save.
4. Confirm: dialog closes, snackbar "Price updated.", list shows the new price.

- [ ] **Step 4: Verify OffShelf path**

1. Toggle a product to `OffShelf`.
2. Click **Modify price**, change price, save. Confirm success.

- [ ] **Step 5: Verify Sold guard**

1. Mark a product as Sold via the existing flow.
2. Confirm the **Modify price** button is NOT rendered for that row.

- [ ] **Step 6: Verify validation**

1. Open the dialog on any non-Sold product, enter `0` or a negative number, click Save.
2. Confirm an inline error message appears and no request is sent (check browser devtools network tab).

- [ ] **Step 7: Verify Serilog audit trail**

In the backend console, confirm log lines like:
```
Admin {guid} changed price of product {guid} (slug-here) from 220 to 259.
```
appear after each successful price change, and not after no-op saves.

- [ ] **Step 8: Stop dev servers**

No commit for this task.

---

## Self-review notes

- **Spec coverage:** Domain method (Task 1), domain tests (Task 2), service + audit (Tasks 3–5), API endpoint (Task 6), frontend API (Task 7), dialog (Task 8), page wiring (Task 9), manual verification (Task 10). All spec sections covered.
- **Sold guard** is enforced in the domain (definitive) AND the UI hides the button (defense in depth). 409 from the API is still mapped to a user-visible error inside the dialog if the row was stale.
- **Audit no-op:** the service avoids logging when price is unchanged, which keeps the log signal meaningful.
- **No placeholders.** Every step shows the exact code/command/expected result.
