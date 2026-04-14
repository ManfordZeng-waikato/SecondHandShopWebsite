# SecondHandShop Frontend

React 19 + TypeScript + Vite SPA for the public storefront and the **`/lord/*`** admin area (JWT in HttpOnly cookies, React Query, MUI 7).

## Tech stack

- React 19, TypeScript, Vite 7
- Material UI 7, Emotion
- React Router 7 (lazy-loaded route chunks)
- TanStack React Query
- Axios (`withCredentials` for admin API)

## Project structure

```text
src/
  app/                  # entry, AppRouter, providers, MainLayout / AdminLayout, themes
  pages/                # route-level screens (public + admin)
  features/             # admin, catalog, home, inquiry (+ analytics under admin)
  entities/             # shared TypeScript types (product, category, customer, …)
  shared/               # http client, config (env), utilities
```

## Quick start

1. Install dependencies:

   ```bash
   npm install
   ```

2. Create **`frontend/.env.local`** (not committed) with at least:

   ```env
   VITE_API_BASE_URL=https://localhost:7266
   VITE_IMAGE_BASE_URL=<Cloudflare Worker or CDN base URL for product images>
   VITE_TURNSTILE_SITE_KEY=<Cloudflare Turnstile site key>
   ```

3. Start the dev server (HTTPS):

   ```bash
   npm run dev
   ```

4. Open **`https://localhost:5173`** (backend should run on **`https://localhost:7266`** so cookies and CORS work).

Other scripts: `npm run build` (tsc + Vite production build), `npm run lint`, `npm run preview`.

## Local HTTPS (mkcert)

- Uses `vite-plugin-mkcert` for local TLS.
- On first run the plugin may install a local CA; restart the browser if you still see certificate warnings.

## Environment variables

| Variable | Purpose |
|----------|---------|
| `VITE_API_BASE_URL` | Backend API origin (default in code: `https://localhost:7266`) |
| `VITE_IMAGE_BASE_URL` | Base URL for product images (e.g. Cloudflare Worker in front of R2) |
| `VITE_TURNSTILE_SITE_KEY` | Cloudflare Turnstile — public inquiry form |

## Routes

**Public**

| Path | Page |
|------|------|
| `/` | Home — hero, featured products, story section |
| `/products` | Product catalog — hierarchical category tabs, search, sort, pagination |
| `/my-story` | Extended “Our story” content |
| `/products/:slug` | Product detail |
| `/products/:id/inquiry` | Inquiry form (Turnstile) |
| `/404` | Not found |

**Admin** (API prefix `/api/lord/*`; app routes use **`/lord`** not `/admin`)

| Path | Page |
|------|------|
| `/lord/login` | Admin login |
| `/lord/change-password` | Forced initial password change (when required) |
| `/lord/products` | Product list & management |
| `/lord/products/new` | Create product |
| `/lord/customers` | Customers |
| `/lord/customers/:customerId` | Customer detail |
| `/lord/analytics` | Analytics overview |

The admin shell includes a **View site** control that opens the public storefront in a **new browser tab**.

## Backend CORS

The API must allow the frontend origin (e.g. `https://localhost:5173`) with **credentials** enabled — see backend `Cors:AllowedOrigins` and the main repo **README**.
