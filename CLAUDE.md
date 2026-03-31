# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

### Frontend (React/Vite)

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

This is a full-stack second-hand goods marketplace with clean architecture across three separate sub-projects.

### Backend (`src/`)

Four .NET 10 projects following clean architecture:

- **Domain** — Entities (`Product`, `ProductImage`, `Category`, `Customer`, `Inquiry`, `InquiryIpCooldown`), enums, `AuditableEntity` base, `SlugValidator`
- **Application** — Use cases via MediatR (CQRS-inspired), DTOs in `Contracts/`, abstractions (interfaces) for persistence, security, storage, messaging, image processing
- **Infrastructure** — EF Core + PostgreSQL implementations: repositories, `SecondHandShopDbContext`, services (JWT, BCrypt, R2 storage, SMTP, Turnstile, remove.bg, admin seeding)
- **WebApi** — ASP.NET Core host, controller-based routing, DI wiring in `Program.cs`

**Admin routes use `/api/lord/*` prefix** (not `/api/admin`) as a security-by-obscurity measure.

Rate-limited endpoints:
- `/api/lord/auth/login` — 5 requests/min per IP
- `/api/products/search` — 30 requests/min per IP (sliding window)

Authentication uses JWT stored in an HttpOnly cookie (`shs.admin.token`).

### Frontend (`frontend/src/`)

React 19 SPA with a feature-sliced structure:

- `app/` — Entry point, `AppRouter.tsx` (React Router 7), `AppProviders.tsx` (React Query + MUI theme), layouts (`MainLayout`, `AdminLayout`), `ProtectedAdminRoute`
- `pages/` — Page-level components (public catalog pages + admin management pages)
- `features/` — Feature modules (`admin`, `catalog`, `home`, `inquiry`) containing React Query hooks and mutations
- `entities/` — TypeScript domain model types
- `shared/api/httpClient.ts` — Axios instance with `withCredentials: true`; auto-redirects to `/lord/login` on 401 from admin endpoints

Mock API fallback: set `VITE_USE_MOCK_API=true` in `frontend/.env.local` to use mock adapters in `shared/mock/` without a running backend.

### Worker (`worker/`)

Cloudflare Worker serving product images from R2 bucket (`patshed`) with caching headers. Handles GET/HEAD requests only.

## Configuration

### Frontend env vars (`frontend/.env.local`)

```
VITE_API_BASE_URL=https://localhost:7266
VITE_USE_MOCK_API=false
VITE_IMAGE_BASE_URL=https://secondhandshop-images.zengchang389.workers.dev
VITE_TURNSTILE_SITE_KEY=<cloudflare turnstile site key>
```

### Backend (`appsettings.Development.json`)

Key sections: `ConnectionStrings`, `Kestrel` (HTTPS port 7266), `Cors:AllowedOrigins`, `Jwt` (key ≥32 chars), `AdminSeed`, `Email:Smtp`, `R2` (Cloudflare account + keys), `RemoveBg`, `CloudflareTurnstile`.

CORS is configured to allow credentials from `https://localhost:5173` in development.

## External Integrations

- **Cloudflare R2** — S3-compatible image storage (`R2ObjectStorageService`)
- **Cloudflare Turnstile** — CAPTCHA on the public inquiry form (`TurnstileValidator`)
- **remove.bg API** — Background removal preview in admin image upload (`RemoveBgService`)
- **SMTP (Gmail)** — Inquiry email notifications, configurable/optional (`SmtpEmailSender` / `NoOpEmailSender`)

## Database

PostgreSQL in development (default connection: `Host=localhost;Database=SecondHandShopDb;Username=postgres;Password=postgres;`). Migrations are in `src/SecondHandShop.Infrastructure/Persistence/Migrations/`. The `AdminSeedService` creates the initial admin user from `AdminSeed` config on first run. Concurrency tokens use PostgreSQL `xmin` system column via `uint RowVersion` properties.

## Local HTTPS

The frontend uses `vite-plugin-mkcert` for local HTTPS on port 5173. The backend Kestrel listens on HTTPS port 7266. Both must use HTTPS because cookies use `SameSite=None; Secure`.
