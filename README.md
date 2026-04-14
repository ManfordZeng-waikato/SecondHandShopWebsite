# SecondHandShop

A production-grade, full-stack **second-hand goods marketplace** built with modern web technologies. The platform provides a polished public storefront for browsing and inquiring about pre-owned items, paired with a comprehensive admin dashboard for inventory, customer, and sales management.

---

## Highlights

- **Clean Architecture** backend with clear separation of concerns across Domain, Application, Infrastructure, and WebApi layers
- **React 19 SPA** with feature-sliced structure, Material UI 7, lazy-loaded routes, and TanStack React Query
- **Hierarchical product categories** on the public catalog (parent / subcategory tabs) with search, sort, and pagination
- **Serverless image delivery** through Cloudflare Workers + R2 object storage
- **Cookie-based JWT authentication** with HttpOnly secure cookies for admin sessions (plus refresh for long-lived admin work)
- **Bot protection** via Cloudflare Turnstile on public-facing forms
- **Rate limiting** on sensitive endpoints (login, search)
- **Background removal** preview powered by remove.bg API for product image editing
- **Admin analytics** surface and **“My story”** public page alongside the core storefront

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Cloudflare Edge                              │
│  ┌──────────────────┐    ┌──────────────────────────────────────┐   │
│  │  Turnstile CAPTCHA│    │  Worker (R2 Image CDN)               │   │
│  └────────┬─────────┘    │  GET/HEAD → R2 Bucket → Cache → User │   │
│           │              └──────────────────────────────────────┘   │
└───────────┼─────────────────────────────────────────────────────────┘
            │
┌───────────▼─────────────────────────────────────────────────────────┐
│  Frontend (React 19 + Vite 7)                                       │
│  ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌───────────────────────┐ │
│  │  Public   │ │  Product  │ │  Inquiry │ │  Admin (Analytics,     │ │
│  │  Catalog  │ │  Detail   │ │  Form    │ │  CRM) /lord/*         │ │
│  └──────────┘ └───────────┘ └──────────┘ └───────────────────────┘ │
└─────────────────────────┬───────────────────────────────────────────┘
                          │ HTTPS (withCredentials)
┌─────────────────────────▼───────────────────────────────────────────┐
│  Backend (ASP.NET Core / .NET 10)                                   │
│  ┌──────────┐ ┌──────────────┐ ┌──────────────┐ ┌───────────────┐ │
│  │  WebApi   │ │  Application │ │Infrastructure│ │    Domain     │ │
│  │Controllers│ │  Use Cases   │ │  EF Core,    │ │  Entities,    │ │
│  │  Filters  │ │  MediatR     │ │  Services    │ │  Rules        │ │
│  └──────────┘ └──────────────┘ └──────────────┘ └───────────────┘ │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
              ┌───────────▼───────────┐
              │      PostgreSQL       │
              │  EF Core Migrations   │
              └───────────────────────┘
```

### Backend — Clean Architecture (.NET 10)

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Domain** | `SecondHandShop.Domain` | Entities (`Product`, `Category`, `Customer`, `Inquiry`, `ProductSale`, etc.), enums, domain validation, `AuditableEntity` base class |
| **Application** | `SecondHandShop.Application` | Use cases via MediatR (CQRS-inspired), DTOs/Contracts, abstraction interfaces for persistence, storage, security, messaging |
| **Infrastructure** | `SecondHandShop.Infrastructure` | EF Core + PostgreSQL, repository implementations, JWT/BCrypt services, R2 storage, SMTP, Turnstile, remove.bg integration |
| **WebApi** | `SecondHandShop.WebApi` | ASP.NET Core host — REST controllers for catalog, inquiries, categories, image helpers, and `/api/lord/*` admin APIs; rate limiting, CORS, Serilog, DI in `Program.cs` |

### Frontend — Feature-Sliced React SPA

| Directory | Purpose |
|-----------|---------|
| `app/` | Entry point, router (React Router 7), providers (React Query + MUI theme), layouts |
| `pages/` | Route screens (public storefront, inquiry, **My story**, 404, and admin under `/lord/*`) |
| `features/` | Feature modules — `admin` (incl. analytics), `catalog`, `home`, `inquiry` — hooks, API calls, and components |
| `entities/` | TypeScript domain model types |
| `shared/` | Axios HTTP client, reusable UI components, utilities |

### Worker — Cloudflare Edge Image CDN

A lightweight Cloudflare Worker that serves product images from an R2 bucket with cache headers (1 day browser / 7 days CDN), CORS support, and ETags. Handles GET/HEAD requests only.

---

## Features

### Public Storefront

- **Home page** — Hero, featured products, “Our story” section, and navigation to the full story page
- **My story** (`/my-story`) — Longer brand narrative
- **Product catalog** (`/products`) — Hierarchical **category** tabs (parent + optional subcategories), full-text search, sort, pagination, fallback messaging when search has no hits
- **Product detail** — Multi-image gallery, condition badges, pricing, path to inquiry
- **Inquiry form** — Turnstile CAPTCHA, auto-customer creation, IP-based cooldown
- **Responsive UI** — Material UI, loading skeletons, code-split route chunks

### Admin Dashboard (`/lord/*`)

- **Secure authentication** — JWT in HttpOnly cookies, optional **session refresh** for long admin sessions, forced initial password change when required
- **Product management** — List/create/edit, status (Available / Sold / Off Shelf), featured flag, **multi-category assignment** (hierarchy-aware)
- **Image upload** — Presigned URLs for direct-to-R2 upload, background removal preview via remove.bg
- **Customer management** — Status workflow (New → Contacted → Qualified → Archived), contact history, notes, detail view
- **Sales tracking** — Sale lifecycle (listed vs final price, payment method, links to customer/inquiry where applicable)
- **Analytics** (`/lord/analytics`) — Sales and inquiry KPIs, monthly trend, category breakdowns, and “hot unsold” / stale listings; backed by `GET /api/lord/analytics/overview` with a user-selected range (e.g. 7d–all time)
- **Email notifications** — Configurable SMTP for inquiry alerts (with no-op fallback)
- **View site** — Toolbar link opens the public storefront in a **new browser tab**

---

## Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Backend** | .NET 10, ASP.NET Core, Entity Framework Core 10, MediatR, BCrypt, JWT Bearer |
| **Database** | PostgreSQL, EF Core Migrations, `xmin` concurrency tokens |
| **Frontend** | React 19, TypeScript, Vite 7, Material UI 7, MUI X Charts, TanStack React Query, Axios, React Router 7 |
| **Image CDN** | Cloudflare Workers, Wrangler 3, R2 (S3-compatible) object storage |
| **Security** | JWT (HttpOnly cookies), Cloudflare Turnstile, BCrypt password hashing, rate limiting |
| **Integrations** | Cloudflare R2, Cloudflare Turnstile, remove.bg API, SMTP (Gmail) |
| **Dev Tools** | vite-plugin-mkcert (local HTTPS), ESLint |

---

## Data Model

```
AdminUser ──────┐
                │ CreatedBy / UpdatedBy
Category ◄─────┼── Product ──┬── ProductImage
                │             ├── ProductSale ──► Customer
                │             └── Inquiry ──────► Customer
                │
                └── InquiryIpCooldown
```

**Key entities:**

- **Category** — Hierarchical (optional parent), used for catalog navigation and many-to-many **product–category** assignments
- **Product** — Title, slug, description, price, condition (LikeNew / Good / Fair / NeedsRepair), status lifecycle (Available → Sold / OffShelf), featured flag with sort order
- **ProductImage** — Cloud storage key, display URL, sort order, primary flag (one per product)
- **Customer** — Auto-created from inquiries, status workflow with admin notes
- **Inquiry** — Links customer to product, tracks email delivery status and retry attempts
- **ProductSale** — Listed vs. final price, payment method, buyer info, linked to customer/inquiry

---

## Repository Structure

```
SecondHandShopWebsite/
├── src/
│   ├── SecondHandShop.Domain/            # Entities, enums, domain rules
│   ├── SecondHandShop.Application/       # Use cases, DTOs, abstractions
│   ├── SecondHandShop.Infrastructure/    # EF Core, repositories, external services; migrations in Migrations/
│   └── SecondHandShop.WebApi/            # Controllers, filters, Program.cs
├── frontend/
│   ├── src/
│   │   ├── app/                          # Router, providers, layouts, theme
│   │   ├── pages/                        # Route screens (public + admin)
│   │   ├── features/                     # admin (incl. analytics), catalog, home, inquiry
│   │   ├── entities/                     # TypeScript domain types
│   │   └── shared/                       # HTTP client, components, utilities
│   └── package.json
├── worker/
│   ├── src/index.ts                      # R2 image CDN handler
│   └── wrangler.toml
├── docs/                                 # Design documentation
├── SecondHandShopWebsite.slnx            # .NET solution file
└── CLAUDE.md                             # AI assistant guidance
```

---

## Getting Started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS)
- [PostgreSQL](https://www.postgresql.org/)

### Backend

```bash
# Restore tools and apply database migrations
dotnet tool restore
dotnet ef database update \
  --project src/SecondHandShop.Infrastructure \
  --startup-project src/SecondHandShop.WebApi

# Run API server (HTTPS on port 7266)
dotnet run --project src/SecondHandShop.WebApi
```

EF Core migration classes are in **`src/SecondHandShop.Infrastructure/Migrations/`** (not under `Persistence/`).

Set the backend secrets via environment variables or `dotnet user-secrets` rather than committing them into `appsettings.Development.json`.

For Supabase Postgres, set `ConnectionStrings__DefaultConnection` to the connection string from the Supabase dashboard. Both standard Npgsql strings and `postgresql://...` URIs are supported.

Use `ConnectionStrings__MigrationConnection` if you want EF Core migrations to use a different connection string than the runtime app. This is useful with Supabase when you run the app through the pooler but run schema migrations through the direct database connection.

PowerShell example:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=YOUR_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

Or with user secrets:

```bash
dotnet user-secrets --project src/SecondHandShop.WebApi set "ConnectionStrings:DefaultConnection" "Host=YOUR_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

Then apply migrations:

```bash
dotnet ef database update --project src/SecondHandShop.Infrastructure --startup-project src/SecondHandShop.WebApi
```

If you manage schema changes separately, disable startup auto-migrations:

```powershell
$env:Database__ApplyMigrationsOnStartup = "false"
```

Also configure JWT key, admin seed credentials, and optional integrations (R2, SMTP, Turnstile, remove.bg).

### Frontend

```bash
cd frontend
npm install
npm run dev    # https://localhost:5173
```

Create `frontend/.env.local` with:

```env
VITE_API_BASE_URL=https://localhost:7266
VITE_IMAGE_BASE_URL=<worker or CDN base URL for product images>
VITE_TURNSTILE_SITE_KEY=<cloudflare turnstile site key>
```

### Worker (optional)

```bash
cd worker
npm install
npx wrangler dev      # Local development
npx wrangler deploy   # Deploy to Cloudflare
```

---

## Security

- **Admin paths** use `/lord` prefix instead of `/admin` to reduce automated scanning
- **JWT tokens** stored in HttpOnly, Secure, SameSite cookies — not accessible via JavaScript; admin UI can call **`/api/lord/auth/refresh`** to renew the session during long work
- **Rate limiting** on login (5/min) and search (30/min) endpoints per IP
- **Turnstile CAPTCHA** on public inquiry form
- **BCrypt** password hashing with forced initial password change
- **HSTS** enforced outside development
- **CORS** restricted to configured origins with credentials support

---

## License

This project is proprietary and confidential. Unauthorized copying or distribution is prohibited.
