import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { getMustChangePassword, isAuthenticated } from '../../features/admin/auth/adminAuth';

export function ProtectedAdminRoute() {
  const location = useLocation();

  if (!isAuthenticated()) {
    return <Navigate to="/lord/login" replace state={{ from: location.pathname }} />;
  }

  if (getMustChangePassword()) {
    return <Navigate to="/lord/change-password" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
