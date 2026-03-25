# SecondHandShop

A full-stack **second-hand goods storefront**: ASP.NET Core Web API backend, React (Vite) frontend, optional **Cloudflare R2** object storage with a **Cloudflare Worker** for public image delivery, and **SQL Server** persistence.

---

## Table of contents

- [Architecture](#architecture)
- [Technology stack](#technology-stack)
- [Repository layout](#repository-layout)
- [Prerequisites](#prerequisites)
- [Backend (ASP.NET Core)](#backend-aspnet-core)
- [Database](#database)
- [Frontend (React + Vite)](#frontend-react--vite)
- [Cloudflare Worker (image CDN)](#cloudflare-worker-image-cdn)
- [Configuration reference](#configuration-reference)
- [HTTP API overview](#http-api-overview)
- [Security and operations notes](#security-and-operations-notes)

---

## Architecture

The server follows a **clean architecture** style split across projects:

| Project | Responsibility |
|--------|----------------|
| `SecondHandShop.Domain` | Entities, domain rules (e.g. products, categories, inquiries) |
| `SecondHandShop.Application` | Use cases, contracts (DTOs), MediatR commands, abstractions |
| `SecondHandShop.Infrastructure` | EF Core, repositories, JWT/password services, R2 (S3-compatible) storage, email, Remove.bg integration |
| `SecondHandShop.WebApi` | HTTP API, authentication, CORS, rate limiting |

The **frontend** is a SPA that talks to the API over HTTPS, with **cookie-based JWT** for admin routes under `/api/lord/*`.

Optional **worker** serves objects from an R2 bucket with caching headers and CORS for GET/HEAD.

---

## Technology stack

**Backend**

- .NET **10** (`net10.0`)
- ASP.NET Core (JWT Bearer, OpenAPI in Development)
- Entity Framework Core **10** + **SQL Server**
- MediatR
- BCrypt (password hashing), AWS SDK S3-compatible client for **Cloudflare R2**

**Frontend**

- React **19**, TypeScript, **Vite** **7**
- MUI (Material UI), Emotion
- TanStack React Query, Axios, React Router **7**
- Local HTTPS via **vite-plugin-mkcert** (default `https://localhost:5173`)

**Worker**

- Cloudflare Workers + **Wrangler** **3**, R2 binding

---

## Repository layout

```text
SecondHandShopWebsite/
  src/
    SecondHandShop.Domain/           # Domain model
    SecondHandShop.Application/      # Application layer
    SecondHandShop.Infrastructure/   # EF Core, external services
    SecondHandShop.WebApi/           # API host (Program.cs, controllers)
  frontend/                        # React SPA
  worker/                          # Cloudflare Worker (R2 GET/HEAD)
  .config/
    dotnet-tools.json              # dotnet-ef tool manifest
```

There is **no** solution (`.sln`) file in the repo; open the folder or the `.csproj` files directly.

---

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (matching `TargetFramework` in `SecondHandShop.WebApi.csproj`)
- [Node.js](https://nodejs.org/) (LTS recommended) for `frontend/` and `worker/`
- **SQL Server** or **SQL Server LocalDB** (default connection string uses LocalDB)
- Optional: [Cloudflare](https://developers.cloudflare.com/) account + R2 bucket + Wrangler for the worker

---

## Backend (ASP.NET Core)

### Run the API

From the repository root:

```bash
cd src/SecondHandShop.WebApi
dotnet run
```

**Default URLs** (see `appsettings.json` / `launchSettings.json`):

- HTTP: `http://localhost:5288`
- HTTPS: `https://localhost:7266`

Use the `https` launch profile if you want both URLs explicitly:

```bash
dotnet run --launch-profile https
```

### Restore EF Core tools (migrations)

The repo pins `dotnet-ef` in `.config/dotnet-tools.json`:

```bash
dotnet tool restore
```

---

## Database

### Connection string

Configure `ConnectionStrings:DefaultConnection` in `appsettings.json`, `appsettings.Development.json`, environment variables, or [user secrets](https://learn.microsoft.com/dotnet/core/tools/user-secrets) for the `SecondHandShop.WebApi` project.

Default template in `appsettings.json` uses **LocalDB**:

`Server=(localdb)\MSSQLLocalDB;Database=SecondHandShopDb;Trusted_Connection=True;TrustServerCertificate=True;`

### Apply migrations

Apply EF Core migrations **from the repo root** (adjust paths if your shell differs):

```bash
dotnet tool restore
dotnet ef database update --project src/SecondHandShop.Infrastructure --startup-project src/SecondHandShop.WebApi
```

Migrations live under `src/SecondHandShop.Infrastructure/Persistence/Migrations/`. A SQL script snapshot is also available at `Persistence/Migrations/InitialCreate.sql` for reference.

### Seed admin user

On startup, `AdminSeedService` ensures an admin user exists using `AdminSeed` settings (see [Configuration reference](#configuration-reference)).

---

## Frontend (React + Vite)

### Install and run

```bash
cd frontend
npm install
npm run dev
```

Default dev server: **`https://localhost:5173`** (strict port; mkcert may prompt to install a local CA).

### Build

```bash
npm run build
npm run preview   # optional production preview
```

### Environment variables (Vite)

Configure via `frontend/.env` or `frontend/.env.local` (not committed). Variables must be prefixed with `VITE_`.

| Variable | Purpose |
|----------|---------|
| `VITE_API_BASE_URL` | Backend base URL (default in code: `https://localhost:7266`) |
| `VITE_USE_MOCK_API` | `true` = mock in-memory data for several features; `false` = real API |
| `VITE_IMAGE_BASE_URL` | Optional prefix for image URLs when needed by the UI |

The shared HTTP client uses **`withCredentials: true`** so **HttpOnly** admin cookies on the API origin are sent for `/api/lord/*` requests.

### Frontend routes

| Path | Description |
|------|-------------|
| `/` | Home |
| `/products` | Product catalog / search |
| `/products/:slug` | Product detail |
| `/products/:id/inquiry` | Inquiry form |
| `/my-story` | Story page |
| `/lord/login` | Admin login |
| `/lord/products` | Admin product list (protected) |
| `/lord/products/new` | New product (protected) |
| `/404` | Not found |

Admin UI uses the **`/lord`** prefix (not `/admin`).

### CORS

The backend must list the frontend origin in `Cors:AllowedOrigins` (e.g. `https://localhost:5173`) and use credentials-compatible CORS (already configured in `Program.cs`).

---

## Cloudflare Worker (image CDN)

The **`worker/`** project exposes **GET/HEAD** for objects in an R2 bucket: path after the host is the object key. It sets cache headers and allows broad CORS for read access.

1. Install dependencies: `cd worker && npm install`
2. Configure `wrangler.toml` (`[[r2_buckets]]`, `bucket_name`, etc.)
3. Local dev: `npm run dev`
4. Deploy: `npm run deploy`

The API stores image keys in the database; **`R2:WorkerBaseUrl`** (or equivalent URL building in `IObjectStorageService`) should match how browsers load images in production.

---

## Configuration reference

Key sections in `appsettings.json` (override per environment or secrets):

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `Kestrel` / `HttpsPort` | URLs and HTTPS port hint |
| `Cors:AllowedOrigins` | Frontend origins for CORS |
| `Jwt` | `Issuer`, `Audience`, `Key` (use a long random key in production) |
| `AdminSeed` | Initial admin username/password (change after first deploy) |
| `Email:Smtp` | SMTP for inquiry notifications; `Enabled: false` uses a no-op sender |
| `R2` | Cloudflare account id, S3 API keys, bucket, **WorkerBaseUrl** for public URLs |
| `RemoveBg` | remove.bg API for admin background-removal preview (`ImageProcessingController`) |

**Never commit production secrets.** Prefer environment variables or user secrets for `Jwt:Key`, SMTP passwords, R2 keys, and Remove.bg keys.

---

## HTTP API overview

**Public catalog**

- `GET /api/categories` — active categories  
- `GET /api/products` — list (optional `categoryId`)  
- `GET /api/products/search` — paged search (rate limited: **SearchRateLimit**)  
- `GET /api/products/featured` — featured products (`limit` clamped)  
- `GET /api/products/slug/{slug}` — product by slug  

**Inquiries**

- `POST /api/inquiries` — create inquiry for a product  

**Admin (JWT + cookie, policy `AdminOnly`)**

- `POST /api/lord/auth/login` — sets HttpOnly cookie `shs.admin.token` (rate limited: **LoginRateLimit**)  
- `POST /api/lord/auth/logout`  
- `GET /api/lord/auth/me`  
- `GET/POST/PUT/... /api/lord/products` — product management (see `AdminProductsController`)  
- `POST /api/lord/images/remove-background-preview` — background removal preview (multipart)  

**Development**

- OpenAPI mapping is enabled when `ASPNETCORE_ENVIRONMENT` is `Development`.

---

## Security and operations notes

- **JWT**: The signing key must be strong and unique in production (`Jwt:Key`).  
- **HTTPS**: Frontend dev uses HTTPS; the API should be reached over HTTPS for cookie `Secure` flags to match.  
- **Rate limiting**: Login and search endpoints use ASP.NET Core rate limiting policies.  
- **HSTS**: Applied outside Development.  
- **Admin paths**: Deliberately under `/lord` to reduce trivial scanning of `/admin`.  

---

## License

If you publish this repository, add a `LICENSE` file and describe terms here.
