# SecondHandShop Frontend

React + TypeScript + Vite frontend skeleton for:
- Public product browsing and inquiry flow
- Admin entry, route guard placeholder, and product management skeleton

## Tech stack

- React 19 + TypeScript
- Vite
- MUI
- React Router
- React Query
- Axios

## Project structure

```text
src/
  app/                  # app entry, routes, theme, layouts, providers
  pages/                # page-level screens
  features/             # feature modules (catalog, inquiry, admin)
  entities/             # domain types (product/category/inquiry)
  shared/               # shared api/config/mock/components
```

## Quick start

1. Install dependencies:
   ```bash
   npm install
   ```
2. Create environment file:
   ```bash
   cp .env.example .env
   ```
3. Start dev server:
   ```bash
   npm run dev
   ```
4. Open:
   - `https://localhost:5173`

## Local HTTPS (mkcert)

- This project uses `vite-plugin-mkcert` to generate and trust local certificates.
- On first run, the plugin may request permission to install a local CA certificate.
- If Chrome still shows a cert warning, restart the browser after first certificate installation.

## Environment variables

- `VITE_API_BASE_URL`: backend API base url (default `https://localhost:7266`)
- `VITE_USE_MOCK_API`: `true` uses in-memory mock adapter, `false` calls backend directly

## Routes

Public:
- `/` - home page with product list and category sidebar
- `/products/:slug` - product detail
- `/products/:id/inquiry` - inquiry form

Admin:
- `/admin/login` - admin login placeholder
- `/admin/products` - admin product status management (guarded)
- `/admin/products/new` - create product form (guarded)

## API integration notes

- Current frontend supports mock fallback via `VITE_USE_MOCK_API=true`.
- For real backend integration, set `VITE_USE_MOCK_API=false` and ensure backend supports:
  - `GET /api/categories`
  - `GET /api/products`
  - `GET /api/products/slug/:slug`
  - `POST /api/inquiries`
  - `POST /api/admin/products`
  - `PUT /api/admin/products/:id/status`

## Backend CORS reminder

When frontend and backend run on different origins, backend must allow frontend origin via CORS policy.
