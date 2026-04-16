# CLAUDE.md

Guidance for Claude Code ([claude.ai/code](https://claude.ai/code)) when working in this repository.

## Commands

### Backend (.NET 10)

```bash
# Run API server (from repo root or WebApi project dir)
dotnet run --project src/SecondHandShop.WebApi

# Apply EF Core migrations
dotnet ef database update --project src/SecondHandShop.Infrastructure --startup-project src/SecondHandShop.WebApi

# Add a new migration
dotnet ef migrations add <MigrationName> --project src/SecondHandShop.Infrastructure --startup-project src/SecondHandShop.WebApi

# Build solution
dotnet build SecondHandShopWebsite.slnx
```

### Frontend (React / Vite)

```bash
cd frontend
npm install
npm run dev       # Dev server at https://localhost:5173
npm run build     # TypeScript check + production build
npm run lint      # ESLint
npm run preview   # Preview production build
```

### Cloudflare Worker

```bash
cd worker
npm install
npx wrangler dev  # Local dev
npx wrangler deploy
```

## Architecture

Full-stack second-hand marketplace using clean architecture across three sub-projects.

### Backend (`src/`)

Four .NET 10 projects:

- **Domain** — Entities (`Product`, `ProductImage`, `ProductSale`, `ProductCategory`, `Category`, `Customer`, `AdminUser`, `Inquiry`, `InquiryIpCooldown`), enums (`ProductStatus`, `ProductCondition`, `SaleRecordStatus`, `SaleCancellationReason`, `CustomerStatus`, `CustomerSource`, `PaymentMethod`, `EmailDeliveryStatus`), `AuditableEntity` base, validators (`SlugValidator`, `EmailAddressSyntaxValidator`), `DomainRuleViolationException`
- **Application** — Use cases via MediatR (CQRS-inspired) in `UseCases/` (Admin, Analytics, Catalog, Categories, Customers, Inquiries, Sales), DTOs in `Contracts/`, abstractions for persistence, security, storage, messaging, image processing
- **Infrastructure** — EF Core + PostgreSQL: 7 repositories, `SecondHandShopDbContext`, services (JWT, BCrypt, R2 storage, SMTP, Turnstile, remove.bg, analytics, admin seeding, catalog seeding, category cache, admin login notifications, inquiry email dispatch)
- **WebApi** — ASP.NET Core host, controller-based routing, DI in `Program.cs`

**Controllers:**

| Controller | Route prefix |
|---|---|
| ProductsController | `api/products` (public) |
| CategoriesController | `api/categories` (public) |
| InquiriesController | `api/inquiries` (public) |
| AdminAuthController | `api/lord/auth` |
| AdminProductsController | `api/lord/products` |
| AdminProductSalesController | `api/lord/products/{productId}` |
| AdminCustomersController | `api/lord/customers` |
| AdminAnalyticsController | `api/lord/analytics` |
| AdminPingController | `api/lord/ping` |
| ImageProcessingController | `api/lord/images` |

**Admin API prefix:** `/api/lord/*` (not `/api/admin`) as a security-by-obscurity measure.

**Rate limits:**

- `/api/lord/auth/login` — 5 requests/min per IP (fixed window)
- `/api/products/search` — 30 requests/min per IP (sliding window, 3 segments)

**Auth:** JWT in HttpOnly cookie `shs.admin.token`. Sliding token renewal (20-min access tokens). Two authorization policies: `AdminSession` (any valid admin JWT) and `AdminFullAccess` (excludes password-change-required tokens).

**Middleware:** ForwardedHeaders, CorrelationId, Serilog request logging, security headers (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy).

**Output caching:** `CategoriesList` and `CategoriesTree` — 5-minute expiry.

### Frontend (`frontend/src/`)

React 19 SPA (MUI 7, TanStack React Query 5, React Router 7, Axios, Vite 7):

- `app/` — `App.tsx`, routes (`AppRouter.tsx`, `ProtectedAdminRoute`, `ProtectedPasswordChangeRoute`), providers (`AppProviders.tsx`), layouts (`MainLayout`, `AdminLayout`), theme, components (`Navbar`)
- `pages/` — Public: `HomePage`, `ProductsPage`, `ProductDetailPage`, `InquiryPage`, `MyStoryPage`, `NotFoundPage`; Admin: `AdminLoginPage`, `AdminProductsPage`, `AdminNewProductPage`, `AdminCustomersPage`, `AdminCustomerDetailPage`, `AdminAnalyticsPage`, `AdminChangePasswordPage`
- `features/` — Modules: `admin` (analytics, api, auth, components), `catalog` (api, hooks, components), `home` (api, components), `inquiry` (api, components)
- `entities/` — TypeScript domain types: `product`, `sale`, `category`, `customer`, `inquiry`
- `shared/` — `api/httpClient.ts` (Axios with `withCredentials: true`; 401 on admin routes redirects to `/lord/login`), `components/` (`StatusChip`), `utils/` (`imageUrl`), `config/` (`env`)

### Worker (`worker/`)

Cloudflare Worker serving product images from R2 bucket `patshed` with cache headers (1-day client, 7-day edge). GET/HEAD/OPTIONS only, CORS enabled.

## Configuration

### Frontend (`frontend/.env.local`)

```
VITE_API_BASE_URL=https://localhost:7266
VITE_IMAGE_BASE_URL=https://secondhandshop-images.zengchang389.workers.dev
VITE_TURNSTILE_SITE_KEY=<cloudflare turnstile site key>
```

### Backend (`appsettings.Development.json`)

Notable keys: `ConnectionStrings`, `Kestrel` (HTTPS 7266), `Cors:AllowedOrigins`, `Jwt` (key ≥ 32 chars), `AdminSeed`, `Email:Smtp`, `R2`, `RemoveBg`, `CloudflareTurnstile`.

CORS allows credentials from `https://localhost:5173` in development.

## External integrations

- **Cloudflare R2** — S3-compatible storage (`R2ObjectStorageService`)
- **Cloudflare Turnstile** — CAPTCHA on public inquiry (`TurnstileValidator`)
- **remove.bg** — Background removal in admin upload (`RemoveBgService`)
- **SMTP (e.g. Gmail)** — Inquiry email; optional (`SmtpEmailSender` / `NoOpEmailSender`)

## Database

PostgreSQL in development (example: `Host=localhost;Database=SecondHandShopDb;Username=postgres;Password=postgres`). Migrations: `src/SecondHandShop.Infrastructure/Migrations/`. Auto-migration and auto-seeding on startup (configurable). `AdminSeedService` creates the initial admin from `AdminSeed`. `CatalogSeedService` seeds default categories. Concurrency: PostgreSQL `xmin` via `uint RowVersion`.

## Local HTTPS

Frontend: `vite-plugin-mkcert` on port 5173. Backend: Kestrel HTTPS 7266. Both need HTTPS for cookies (`SameSite=None; Secure`).

---

## Git commit conventions (mandatory)

After code changes, produce a commit message and stage/commit in Git.

### Message format

```
<type>(<scope>): <short summary>
```

- **type:** `feat` | `fix` | `refactor` | `perf` | `docs` | `style` | `test` | `chore`
- **scope:** `product` | `sale` | `customer` | `api` | `frontend` | `db` | `auth` (or the closest fit)
- **summary:** present tense, ≤ 72 characters, describes the change

### Commit body

For non-trivial changes, add a body covering: why, what changed, and important side effects.

### Multiple logical changes

Split into separate commits when changes are logically distinct.

### Avoid vague summaries

Do not use messages like: "update code", "fix stuff", "changes".

### Example

```
feat(sale): add product sale history tracking

- introduce ProductSale status field
- support cancelling sale instead of overwriting
- enforce single active sale per product
```

---

## Usage safety and session handoff

When remaining daily usage is at or below **10%**, stop the current implementation at the next safe point.

Before stopping:

1. Summarize progress clearly.
2. Record everything needed to resume in a new session.
3. List completed work, remaining work, blockers, assumptions, and next steps.
4. Save that to a continuation file in the repo.
5. Tell the user the file path and name.

### Safe stopping

- Do not start large refactors, cross-file rewrites, or risky partial edits when usage is low.
- Prefer stopping at a compilable, consistent checkpoint.
- If mid-task, finish the smallest safe unit, then stop.

### Continuation file location

Directory: `docs/session-handoff/`

File name:

`handoff-YYYY-MM-DD-HHMM-<short-task-name>.md`

Example: `handoff-2026-04-13-1430-product-search-api.md`

### Handoff template

```md
# Session Handoff

## Task
<short description of the task>

## Current Status
<what has already been completed>

## Files Changed
- <file path>
- <file path>

## Key Context
<important architecture, assumptions, constraints, business rules, environment details>

## Remaining Work
- <remaining item>
- <remaining item>

## Blockers / Risks
- <risk or blocker>

## Suggested Next Prompt
<recommended prompt to paste next time for fast continuation>
```
