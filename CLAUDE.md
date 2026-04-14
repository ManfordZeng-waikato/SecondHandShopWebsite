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

- **Domain** — Entities (`Product`, `ProductImage`, `Category`, `Customer`, `Inquiry`, `InquiryIpCooldown`), enums, `AuditableEntity` base, `SlugValidator`
- **Application** — Use cases via MediatR (CQRS-inspired), DTOs in `Contracts/`, abstractions (interfaces) for persistence, security, storage, messaging, image processing
- **Infrastructure** — EF Core + PostgreSQL: repositories, `SecondHandShopDbContext`, services (JWT, BCrypt, R2 storage, SMTP, Turnstile, remove.bg, admin seeding)
- **WebApi** — ASP.NET Core host, controller-based routing, DI in `Program.cs`

**Admin API prefix:** `/api/lord/*` (not `/api/admin`) as a security-by-obscurity measure.

**Rate limits:**

- `/api/lord/auth/login` — 5 requests/min per IP
- `/api/products/search` — 30 requests/min per IP (sliding window)

**Auth:** JWT in HttpOnly cookie `shs.admin.token`.

### Frontend (`frontend/src/`)

React 19 SPA, feature-sliced layout:

- `app/` — Entry, `AppRouter.tsx` (React Router 7), `AppProviders.tsx` (React Query + MUI), layouts (`MainLayout`, `AdminLayout`), `ProtectedAdminRoute`
- `pages/` — Public catalog and admin pages
- `features/` — Modules (`admin`, `catalog`, `home`, `inquiry`): React Query hooks and mutations
- `entities/` — TypeScript domain types
- `shared/api/httpClient.ts` — Axios with `withCredentials: true`; 401 on admin routes redirects to `/lord/login`

### Worker (`worker/`)

Cloudflare Worker serving product images from R2 bucket `patshed` with cache headers. GET/HEAD only.

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

PostgreSQL in development (example: `Host=localhost;Database=SecondHandShopDb;Username=postgres;Password=postgres`). Migrations: `src/SecondHandShop.Infrastructure/Migrations/`. `AdminSeedService` creates the initial admin from `AdminSeed` on first run. Concurrency: PostgreSQL `xmin` via `uint RowVersion`.

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
