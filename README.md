# SecondHandShop

A production-grade, full-stack **second-hand goods marketplace** built with modern web technologies. The platform provides a polished public storefront for browsing and inquiring about pre-owned items, paired with a comprehensive admin dashboard for inventory, customer, and sales management.

---

## Highlights

- **Clean Architecture** backend with clear separation of concerns across Domain, Application, Infrastructure, and WebApi layers
- **React 19 SPA** with feature-sliced structure, Material UI, and server-state management via React Query
- **Serverless image delivery** through Cloudflare Workers + R2 object storage
- **Cookie-based JWT authentication** with HttpOnly secure cookies for admin sessions
- **Bot protection** via Cloudflare Turnstile on public-facing forms
- **Rate limiting** on sensitive endpoints (login, search)
- **Background removal** preview powered by remove.bg API for product image editing
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
│  │  Public   │ │  Product  │ │  Inquiry │ │  Admin Dashboard      │ │
│  │  Catalog  │ │  Detail   │ │  Form    │ │  /lord/* (protected)  │ │
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
| **WebApi** | `SecondHandShop.WebApi` | ASP.NET Core host, 9 controllers, rate limiting, CORS, DI composition in `Program.cs` |

### Frontend — Feature-Sliced React SPA

| Directory | Purpose |
|-----------|---------|
| `app/` | Entry point, router (React Router 7), providers (React Query + MUI theme), layouts |
| `pages/` | 6 public pages + 6 admin pages |
| `features/` | Feature modules — `admin`, `catalog`, `home`, `inquiry` — each with hooks, API calls, and components |
| `entities/` | TypeScript domain model types |
| `shared/` | Axios HTTP client, reusable UI components, utilities |

### Worker — Cloudflare Edge Image CDN

A lightweight Cloudflare Worker that serves product images from an R2 bucket with cache headers (1 day browser / 7 days CDN), CORS support, and ETags. Handles GET/HEAD requests only.

---

## Features

### Public Storefront

- **Home page** with hero section, featured product carousel, and brand story
- **Product catalog** with category filtering, full-text search, sorting, and pagination
- **Product detail** pages with multi-image gallery, condition badges, and pricing
- **Inquiry form** with Turnstile CAPTCHA, auto-customer creation, and IP-based cooldown
- **Responsive design** with Material UI components and loading skeletons

### Admin Dashboard 

- **Secure authentication** — JWT in HttpOnly cookies, forced initial password change
- **Product management** — Create, update status (Available / Sold / Off Shelf), toggle featured, manage images
- **Image upload** — Presigned S3 URLs for direct-to-R2 upload, background removal preview via remove.bg
- **Customer management** — Status workflow (New → Contacted → Qualified → Archived), contact history, notes
- **Sales tracking** — Record sale price, payment method (Cash / Bank Transfer / Card / Other), link to customer and inquiry
- **Email notifications** — Configurable SMTP for inquiry alerts (with no-op fallback)

---

## Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Backend** | .NET 10, ASP.NET Core, Entity Framework Core 10, MediatR, BCrypt, JWT Bearer |
| **Database** | PostgreSQL, EF Core Migrations, `xmin` concurrency tokens |
| **Frontend** | React 19, TypeScript, Vite 7, Material UI 7, TanStack React Query, Axios, React Router 7 |
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
│   ├── SecondHandShop.Infrastructure/    # EF Core, repositories, external services
│   └── SecondHandShop.WebApi/            # Controllers, filters, Program.cs
├── frontend/
│   ├── src/
│   │   ├── app/                          # Router, providers, layouts, theme
│   │   ├── pages/                        # 12 page components
│   │   ├── features/                     # admin, catalog, home, inquiry
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

Configure `appsettings.Development.json` for database connection, JWT key, admin seed credentials, and optional integrations (R2, SMTP, Turnstile, remove.bg).

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
- **JWT tokens** stored in HttpOnly, Secure, SameSite cookies — not accessible via JavaScript
- **Rate limiting** on login (5/min) and search (30/min) endpoints per IP
- **Turnstile CAPTCHA** on public inquiry form
- **BCrypt** password hashing with forced initial password change
- **HSTS** enforced outside development
- **CORS** restricted to configured origins with credentials support

---

## License

This project is proprietary and confidential. Unauthorized copying or distribution is prohibited.
