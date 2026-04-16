# SecondHandShop Testing Strategy

## Goals

- Follow the existing Clean Architecture boundaries: `Domain -> Application -> Infrastructure -> WebApi`.
- Stay close to the current service-based style instead of forcing feature-slice test conventions that do not match the codebase.
- Optimize for maintainability: reusable fixtures, clear project boundaries, and business-oriented test names.

## Proposed Test Tree

```text
SecondHandShopWebsite
|-- src
|   |-- SecondHandShop.Domain
|   |-- SecondHandShop.Application
|   |-- SecondHandShop.Infrastructure
|   `-- SecondHandShop.WebApi
|-- tests
|   |-- Directory.Build.props
|   |-- SecondHandShop.Domain.UnitTests
|   |   `-- Entities
|   |       |-- CustomerTests.cs
|   |       `-- ProductTests.cs
|   |-- SecondHandShop.Application.UnitTests
|   |   `-- UseCases
|   |       |-- Admin
|   |       |   `-- LoginAdminCommandHandlerTests.cs
|   |       |-- Customers
|   |       |   `-- CustomerResolutionServiceTests.cs
|   |       `-- Inquiries
|   |           `-- InquiryServiceTests.cs
|   `-- SecondHandShop.WebApi.IntegrationTests
|       |-- Controllers
|       |   `-- AdminAuthAndAuthorizationTests.cs
|       `-- Infrastructure
|           `-- TestWebApplicationFactory.cs
|-- frontend
|   |-- playwright.config.ts
|   |-- src
|   |   |-- features
|   |   |   `-- admin
|   |   |       `-- components
|   |   |           `-- __tests__
|   |   |               `-- ProductSaleDialog.test.tsx
|   |   `-- test
|   |       |-- renderWithProviders.tsx
|   |       `-- setup.ts
|   `-- tests
|       `-- e2e
|           `-- admin-and-public-flows.spec.ts
`-- docs
    `-- testing-strategy.md
```

## Layer Responsibilities

### 1. Domain Unit Tests

Scope:
- Pure business rules inside aggregates and entities.
- No EF Core, no DI container, no HTTP, no external services.
- Fastest test layer and first safety net for product/customer lifecycle rules.

Business examples:
- Product marked as sold should create a sale history row, clear featured state, and point `CurrentSaleId` to the new sale.
- Reverting a sold product should cancel the current sale and move the product back to `Available`.
- A sold product must not be moved directly to `OffShelf`.

### 2. Application Unit Tests

Scope:
- Service/handler behavior in `Application` with repository and gateway mocks.
- Validate orchestration, branching rules, and side effects such as `SaveChangesAsync`, notifications, or conflict handling.
- Best place for `InquiryService`, `CustomerResolutionService`, `AdminSaleService`, `LoginAdminCommandHandler`.

Business examples:
- Valid inquiry should pass Turnstile, create or resolve a customer, persist inquiry, commit transaction, and notify dispatcher.
- Repeated inquiry from the same IP/product window should create cooldown state and block submission.
- Successful admin login should reset lockout counters, persist audit fields, create JWT, and enqueue login notification.

### 3. Web API Integration Tests

Scope:
- Real ASP.NET Core request pipeline via `WebApplicationFactory<Program>`.
- Covers routing, filters, auth, authorization policies, cookie behavior, model binding, and response contracts.
- Replace only unstable infrastructure dependencies through DI overrides.

Business examples:
- `/api/lord/products` should return `401` when there is no admin session.
- `AdminFullAccess` endpoints should return `403` for tokens marked with `pwd_chg_req=true`.
- `/api/lord/auth/logout` should return `204` and clear the admin auth cookie.

### 4. Frontend Component Tests

Scope:
- React component behavior with Vitest + Testing Library.
- Focus on business interaction, validation, and API contract wiring instead of CSS snapshots.
- Mock network adapters, keep React Query provider real.

Business examples:
- `ProductSaleDialog` should block submit when final sold price is negative.
- Entering buyer email/phone without selecting an existing customer should show auto-create/match guidance.
- Successful sale submission should send trimmed payload and trigger `onSaved`.

### 5. E2E Tests

Scope:
- Critical cross-layer flows through the browser with Playwright.
- Run against an explicitly configured local/staging environment.
- Keep the set small and business-critical to avoid brittle suites.

Business examples:
- Admin can sign in and land on `/lord/products`.
- Public catalog page loads browse/search surface successfully.
- Inquiry page blocks submission until the user provides at least one contact method.

## Recommended Tooling

### Backend

- `xUnit`
- `Moq`
- `FluentAssertions`
- `coverlet.collector`

### API Integration

- `Microsoft.AspNetCore.Mvc.Testing`
- `WebApplicationFactory<Program>`

### Frontend

- `Vitest`
- `@testing-library/react`
- `@testing-library/user-event`
- `@testing-library/jest-dom`

### E2E

- `@playwright/test`

## Priority Backlog By Module

### Product

P0:
- create product with unique slug and active category
- edit product detail fields without breaking slug normalization
- `Available -> OffShelf -> Available` status path
- mark sold via sale flow instead of direct status update
- revert sale preserves immutable sale history

P1:
- featuring allowed only for `Available`
- image denormalization updates cover key and image count

P2:
- soft delete

Note:
- I did not find a current product soft delete implementation in `Domain`, `Application`, or `WebApi`.
- Treat soft delete as a planned contract: add failing tests first when the feature is introduced, instead of inventing behavior that the current codebase does not implement.

### Inquiry

P0:
- submit inquiry for available product in active category
- reject inquiry when Turnstile fails
- enforce IP/email/message anti-spam windows
- create or merge customer from inquiry contact data

P1:
- persist cooldown after repeated rate-limit violations
- notify dispatcher after successful persistence

### Admin

P0:
- login success path with audit fields and notification
- invalid password increments failed count
- lockout after repeated failed logins
- full-access endpoints reject password-change-required tokens

P1:
- refresh session renews cookie/header
- logout clears cookie

### Customer

P0:
- inquiry source creates customer with `PrimarySource = Inquiry`
- sale source creates customer with `PrimarySource = Sale`
- email + phone collision across two customers returns conflict
- merge fills blank contact fields without overwriting known data

P1:
- admin edit flow updates status/notes without breaking contact validation
- customer detail aggregates inquiry + sale history correctly

## Execution Order

1. Keep expanding `Domain.UnitTests` and `Application.UnitTests` first. They are the cheapest regression net.
2. Add `WebApi.IntegrationTests` around auth, product sale endpoints, and inquiry submission contracts.
3. Cover high-churn admin dialogs and inquiry form with component tests.
4. Keep Playwright focused on 3-5 golden paths only.

## Suggested CI Commands

```powershell
dotnet test tests\SecondHandShop.Domain.UnitTests\SecondHandShop.Domain.UnitTests.csproj
dotnet test tests\SecondHandShop.Application.UnitTests\SecondHandShop.Application.UnitTests.csproj
dotnet test tests\SecondHandShop.WebApi.IntegrationTests\SecondHandShop.WebApi.IntegrationTests.csproj
npm --prefix frontend run test
npm --prefix frontend run test:e2e
```
