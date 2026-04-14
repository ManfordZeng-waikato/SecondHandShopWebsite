import { lazy, Suspense } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { CircularProgress, Box } from '@mui/material';
import { AdminLayout } from '../layouts/AdminLayout';
import { MainLayout } from '../layouts/MainLayout';
import { AdminAuthProvider } from '../../features/admin/auth/adminAuth';
import { ProtectedAdminRoute } from './ProtectedAdminRoute';
import { ProtectedPasswordChangeRoute } from './ProtectedPasswordChangeRoute';

const HomePage = lazy(() =>
  import('../../pages/HomePage').then((m) => ({ default: m.HomePage })),
);
const ProductsPage = lazy(() =>
  import('../../pages/ProductsPage').then((m) => ({ default: m.ProductsPage })),
);
const MyStoryPage = lazy(() =>
  import('../../pages/MyStoryPage').then((m) => ({ default: m.MyStoryPage })),
);
const ProductDetailPage = lazy(() =>
  import('../../pages/ProductDetailPage').then((m) => ({ default: m.ProductDetailPage })),
);
const InquiryPage = lazy(() =>
  import('../../pages/InquiryPage').then((m) => ({ default: m.InquiryPage })),
);
const NotFoundPage = lazy(() =>
  import('../../pages/NotFoundPage').then((m) => ({ default: m.NotFoundPage })),
);

const AdminLoginPage = lazy(() =>
  import('../../pages/AdminLoginPage').then((m) => ({ default: m.AdminLoginPage })),
);
const AdminChangePasswordPage = lazy(() =>
  import('../../pages/AdminChangePasswordPage').then((m) => ({ default: m.AdminChangePasswordPage })),
);
const AdminCustomersPage = lazy(() =>
  import('../../pages/AdminCustomersPage').then((m) => ({ default: m.AdminCustomersPage })),
);
const AdminCustomerDetailPage = lazy(() =>
  import('../../pages/AdminCustomerDetailPage').then((m) => ({
    default: m.AdminCustomerDetailPage,
  })),
);
const AdminProductsPage = lazy(() =>
  import('../../pages/AdminProductsPage').then((m) => ({ default: m.AdminProductsPage })),
);
const AdminNewProductPage = lazy(() =>
  import('../../pages/AdminNewProductPage').then((m) => ({ default: m.AdminNewProductPage })),
);
const AdminAnalyticsPage = lazy(() =>
  import('../../pages/AdminAnalyticsPage').then((m) => ({ default: m.AdminAnalyticsPage })),
);

function PageFallback() {
  return (
    <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
      <CircularProgress />
    </Box>
  );
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <AdminAuthProvider>
        <Suspense fallback={<PageFallback />}>
          <Routes>
            <Route
              path="/"
              element={
                <MainLayout fullWidth>
                  <HomePage />
                </MainLayout>
              }
            />
            <Route
              path="/products"
              element={
                <MainLayout fullWidth>
                  <ProductsPage />
                </MainLayout>
              }
            />
            <Route
              path="/my-story"
              element={
                <MainLayout>
                  <MyStoryPage />
                </MainLayout>
              }
            />
            <Route
              path="/products/:slug"
              element={
                <MainLayout>
                  <ProductDetailPage />
                </MainLayout>
              }
            />
            <Route
              path="/products/:id/inquiry"
              element={
                <MainLayout>
                  <InquiryPage />
                </MainLayout>
              }
            />
            <Route
              path="/lord/login"
              element={
                <MainLayout>
                  <AdminLoginPage />
                </MainLayout>
              }
            />
            <Route element={<ProtectedPasswordChangeRoute />}>
              <Route
                path="/lord/change-password"
                element={
                  <MainLayout>
                    <AdminChangePasswordPage />
                  </MainLayout>
                }
              />
            </Route>
            <Route element={<ProtectedAdminRoute />}>
              <Route
                path="/lord/customers"
                element={
                  <AdminLayout>
                    <AdminCustomersPage />
                  </AdminLayout>
                }
              />
              <Route
                path="/lord/customers/:customerId"
                element={
                  <AdminLayout>
                    <AdminCustomerDetailPage />
                  </AdminLayout>
                }
              />
              <Route
                path="/lord/products"
                element={
                  <AdminLayout>
                    <AdminProductsPage />
                  </AdminLayout>
                }
              />
              <Route
                path="/lord/products/new"
                element={
                  <AdminLayout>
                    <AdminNewProductPage />
                  </AdminLayout>
                }
              />
              <Route
                path="/lord/analytics"
                element={
                  <AdminLayout>
                    <AdminAnalyticsPage />
                  </AdminLayout>
                }
              />
            </Route>
            <Route path="/404" element={<NotFoundPage />} />
            <Route path="*" element={<Navigate to="/404" replace />} />
          </Routes>
        </Suspense>
      </AdminAuthProvider>
    </BrowserRouter>
  );
}
