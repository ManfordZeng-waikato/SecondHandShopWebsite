import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { isAdminLoggedIn } from '../../features/admin/auth/adminSession';

export function ProtectedAdminRoute() {
  const location = useLocation();

  if (!isAdminLoggedIn()) {
    return <Navigate to="/admin/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
