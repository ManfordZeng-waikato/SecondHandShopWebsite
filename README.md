# SecondHandShopWebsite

A full-stack second-hand marketplace for browsing pre-owned products, submitting purchase inquiries, and managing back-office inventory, customers, sales, images, and analytics.

The project combines an ASP.NET Core clean-architecture backend, a React/Vite single-page application, PostgreSQL persistence, and a small Cloudflare Worker used as the public image delivery layer.

## Project Goals

- Provide a polished public storefront for second-hand goods.
- Keep product, customer, inquiry, and sale workflows manageable from a private admin dashboard.
- Store source-of-truth business data in PostgreSQL while serving product images from object storage.
- Use secure cookie-based admin authentication and explicit CORS configuration for the SPA/backend split.
- Keep external integrations optional where possible, so local development can run without every production service enabled.

## Main Features

### Public Storefront

- Home page with hero content, featured products, and story sections.
- Product catalog with category filtering, search, sorting, pagination, loading skeletons, and empty-state handling.
- Product detail page with image gallery, price, condition, status, and inquiry entry point.
- Hierarchical category navigation based on parent and child categories.
- Public inquiry form protected by Cloudflare Turnstile.
- "My Story" page for brand or shop background content.
- Responsive UI built with Material UI and route-based SPA navigation.

### Admin Dashboard

- Admin login, logout, session refresh, and current-user lookup.
- Forced initial password-change flow for seeded or reset admin users.
- Product listing and creation workflows.
- Product status management for available, sold, and off-shelf inventory.
- Featured product controls and display ordering.
- Multi-category product assignment with hierarchy-aware selection.
- Direct-to-object-storage image upload using presigned URLs.
- Image metadata management, primary image selection, and deletion.
- remove.bg-powered background-removal preview endpoint for product images.
- Customer list, customer creation, customer editing, status management, and customer detail views.
- Inquiry history linked to products and customers.
- Product sale recording, sale history, active sale lookup, and sale reversal.
- Analytics dashboard with KPI cards, sales trends, demand by category, sales by category, top categories, and hot unsold listings.

### Integrations

- Cloudflare R2 for product image object storage.
- Cloudflare Worker for public image serving and caching.
- Cloudflare Turnstile for public inquiry bot protection.
- remove.bg for optional image background-removal previews.
- SMTP through MailKit for inquiry/admin notification email.
- BCrypt for admin password hashing.
- JWT Bearer authentication using an HttpOnly admin cookie.

## Architecture

The repository is split into three main runtime areas:

- `src/`: ASP.NET Core backend using clean architecture.
- `frontend/`: React 19 SPA served by Vite during development.
- `worker/`: Cloudflare Worker that serves R2-hosted product images.

High-level request flow:

```text
Browser
  |
  |-- React SPA (public pages and  admin pages)
  |
  |-- HTTPS API calls with credentials
  v
ASP.NET Core WebApi
  |
  |-- Application use cases and domain rules
  |-- Infrastructure services and repositories
  |
  v
PostgreSQL

Product images:

Browser -> Cloudflare Worker -> Cloudflare R2
```

### Backend Layout

The backend follows a clean architecture layout:

- `SecondHandShop.Domain`
  - Domain entities such as `Product`, `ProductImage`, `ProductSale`, `ProductCategory`, `Category`, `Customer`, `AdminUser`, `Inquiry`, and `InquiryIpCooldown`.
  - Domain enums such as `ProductStatus`, `ProductCondition`, `CustomerStatus`, `PaymentMethod`, `SaleRecordStatus`, and `EmailDeliveryStatus`.
  - Shared domain utilities such as slug and email syntax validation.

- `SecondHandShop.Application`
  - MediatR-based admin authentication use cases.
  - Catalog, category, inquiry, customer, sale, and analytics use cases.
  - Request/response contracts and DTOs.
  - Abstractions for repositories, storage, security, messaging, image processing, and common infrastructure services.

- `SecondHandShop.Infrastructure`
  - EF Core `SecondHandShopDbContext` and PostgreSQL mappings.
  - Repository implementations.
  - EF Core migrations.
  - JWT, BCrypt, R2, SMTP, Turnstile, remove.bg, analytics, seeding, category cache, and inquiry dispatch services.

- `SecondHandShop.WebApi`
  - ASP.NET Core host, controllers, filters, middleware, CORS, rate limiting, authentication, authorization, output caching, and Serilog setup.

### API Surface

Public APIs:

- `GET /api/products/search`
- `GET /api/products/featured`
- `GET /api/products/{id}`
- `GET /api/products/slug/{slug}`
- `GET /api/categories`
- `GET /api/categories/tree`
- `POST /api/inquiries`

Admin APIs:

- `POST /api/lord/auth/login`
- `POST /api/lord/auth/refresh`
- `POST /api/lord/auth/logout`
- `POST /api/lord/auth/change-initial-password`
- `GET /api/lord/auth/me`
- `GET /api/lord/products`
- `POST /api/lord/products`
- `PUT /api/lord/products/{productId}/status`
- `PUT /api/lord/products/{productId}/featured`
- `GET /api/lord/products/{productId}/categories`
- `PUT /api/lord/products/{productId}/categories`
- `POST /api/lord/products/{productId}/images/presigned-url`
- `POST /api/lord/products/{productId}/images`
- `DELETE /api/lord/products/{productId}/images/{imageId}`
- `GET /api/lord/products/{productId}/sale`
- `GET /api/lord/products/{productId}/sales`
- `GET /api/lord/products/{productId}/inquiries`
- `POST /api/lord/products/{productId}/mark-sold`
- `POST /api/lord/products/{productId}/revert-sale`
- `GET /api/lord/customers`
- `POST /api/lord/customers`
- `GET /api/lord/customers/{customerId}`
- `PATCH /api/lord/customers/{customerId}`
- `GET /api/lord/customers/{customerId}/inquiries`
- `GET /api/lord/customers/{customerId}/sales`
- `GET /api/lord/analytics/overview`
- `POST /api/lord/images/remove-background-preview`
- `GET /api/lord/ping`

### Frontend Layout

The frontend uses a feature-oriented structure:

- `app/`: application shell, router, providers, layouts, theme, and top-level components.
- `pages/`: route-level screens for public and admin pages.
- `features/admin`: admin API clients, auth state, analytics components, product/customer/sale dialogs, and image upload components.
- `features/catalog`: product catalog API, filters, product grid, category tabs, sort controls, pagination, and loading states.
- `features/home`: home-page API and marketing/product sections.
- `features/inquiry`: inquiry API and Turnstile widget integration.
- `entities`: shared TypeScript domain types for products, categories, customers, sales, and inquiries.
- `shared`: Axios HTTP client, environment config, reusable UI components, and utilities.

### Worker Layout

The Cloudflare Worker in `worker/` exposes a small image delivery endpoint:

- Serves product images from the configured R2 bucket.
- Allows `GET`, `HEAD`, and `OPTIONS`.
- Adds cache headers for browser and edge caching.
- Supports CORS for public image access.

## Technology Stack

Backend:

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL with Npgsql
- MediatR
- Serilog
- JWT Bearer authentication
- BCrypt.Net
- MailKit
- AWS S3 SDK for Cloudflare R2-compatible storage

Frontend:

- React 19
- TypeScript 5
- Vite 7
- Material UI 7
- MUI X Charts
- TanStack React Query 5
- React Router 7
- Axios
- Vitest, Testing Library, Playwright, and MSW for tests

Cloud and external services:

- Cloudflare R2
- Cloudflare Workers
- Cloudflare Turnstile
- remove.bg
- SMTP email provider

Developer tooling:

- ESLint
- vite-plugin-mkcert for local HTTPS
- Wrangler for Worker development and deployment
- EF Core migrations

## Data Model Overview

Core relationships:

```text
Category
  |-- child categories
  |-- many products through ProductCategory

Product
  |-- many ProductImage records
  |-- many Inquiry records
  |-- many ProductSale records

Customer
  |-- many Inquiry records
  |-- many ProductSale records

AdminUser
  |-- audit ownership for created/updated records
```

Important domain concepts:

- Products have a lifecycle status and condition.
- Categories support hierarchy and many-to-many product assignment.
- Product images keep storage keys, display URLs, sort order, and primary-image state.
- Customers can be created from inquiries or managed directly by admins.
- Inquiries link customers to products and track email delivery state.
- Sales preserve listed price, final sale price, payment method, customer/inquiry links, and cancellation status.
- PostgreSQL `xmin` is used as a concurrency token where configured.

## Security and Operational Notes

- Admin authentication uses JWTs stored in an HttpOnly cookie named `shs.admin.token`.
- Admin tokens use sliding renewal and can be invalidated through token-version checks.
- `AdminSession` allows valid admin JWTs, while `AdminFullAccess` excludes password-change-required tokens.
- CORS requires explicit configured origins and supports credentials for the SPA.
- Login is rate-limited to 5 requests per minute per IP.
- Product search is rate-limited to 30 requests per minute per IP.
- Category list/tree responses use ASP.NET Core output caching.
- Security headers, forwarded headers, correlation IDs, exception filters, and Serilog request logging are configured in the Web API.
- Public inquiry submission uses Turnstile validation and IP cooldown tracking.
- Production secrets should be supplied through environment variables, user secrets, or the deployment platform, not committed configuration files.

## Repository Structure

```text
SecondHandShopWebsite/
  src/
    SecondHandShop.Domain/
    SecondHandShop.Application/
    SecondHandShop.Infrastructure/
    SecondHandShop.WebApi/
  frontend/
    src/
      app/
      pages/
      features/
      entities/
      shared/
  worker/
    src/
  docs/
  scripts/
  SecondHandShopWebsite.slnx
```

## Quick Start

Prerequisites:

- .NET SDK 10
- Node.js LTS
- PostgreSQL
- Optional Cloudflare, remove.bg, and SMTP credentials for integration features

Backend:

```bash
dotnet restore SecondHandShopWebsite.slnx
dotnet ef database update --project src/SecondHandShop.Infrastructure --startup-project src/SecondHandShop.WebApi
dotnet run --project src/SecondHandShop.WebApi
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Worker:

```bash
cd worker
npm install
npm run dev
```

Typical local URLs:

- Frontend: `https://localhost:5173`
- Backend API: `https://localhost:7266`

Minimum local configuration usually includes:

- Backend connection string, JWT settings, CORS origins, and admin seed settings.
- Frontend `VITE_API_BASE_URL`, `VITE_IMAGE_BASE_URL`, and `VITE_TURNSTILE_SITE_KEY`.
- Optional R2, SMTP, Turnstile secret, and remove.bg API settings for full integration behavior.

## License

This project is proprietary and confidential. Unauthorized copying or distribution is prohibited.
