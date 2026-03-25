import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminLayout } from '../layouts/AdminLayout';
import { MainLayout } from '../layouts/MainLayout';
import { AdminCustomerDetailPage } from '../../pages/AdminCustomerDetailPage';
import { AdminCustomersPage } from '../../pages/AdminCustomersPage';
import { AdminLoginPage } from '../../pages/AdminLoginPage';
import { AdminNewProductPage } from '../../pages/AdminNewProductPage';
import { AdminProductsPage } from '../../pages/AdminProductsPage';
import { HomePage } from '../../pages/HomePage';
import { InquiryPage } from '../../pages/InquiryPage';
import { MyStoryPage } from '../../pages/MyStoryPage';
import { NotFoundPage } from '../../pages/NotFoundPage';
import { ProductDetailPage } from '../../pages/ProductDetailPage';
import { ProductsPage } from '../../pages/ProductsPage';
import { ProtectedAdminRoute } from './ProtectedAdminRoute';

export function AppRouter() {
  return (
    <BrowserRouter>
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
        </Route>
        <Route path="/404" element={<NotFoundPage />} />
        <Route path="*" element={<Navigate to="/404" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
