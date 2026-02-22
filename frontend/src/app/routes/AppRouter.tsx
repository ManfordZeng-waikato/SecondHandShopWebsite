import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AdminLayout } from '../layouts/AdminLayout';
import { MainLayout } from '../layouts/MainLayout';
import { AdminLoginPage } from '../../pages/AdminLoginPage';
import { AdminNewProductPage } from '../../pages/AdminNewProductPage';
import { AdminProductsPage } from '../../pages/AdminProductsPage';
import { HomePage } from '../../pages/HomePage';
import { InquiryPage } from '../../pages/InquiryPage';
import { MyStoryPage } from '../../pages/MyStoryPage';
import { NotFoundPage } from '../../pages/NotFoundPage';
import { ProductDetailPage } from '../../pages/ProductDetailPage';
import { ProtectedAdminRoute } from './ProtectedAdminRoute';

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/"
          element={
            <MainLayout>
              <HomePage />
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
          path="/admin/login"
          element={
            <MainLayout>
              <AdminLoginPage />
            </MainLayout>
          }
        />
        <Route element={<ProtectedAdminRoute />}>
          <Route
            path="/admin/products"
            element={
              <AdminLayout>
                <AdminProductsPage />
              </AdminLayout>
            }
          />
          <Route
            path="/admin/products/new"
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
