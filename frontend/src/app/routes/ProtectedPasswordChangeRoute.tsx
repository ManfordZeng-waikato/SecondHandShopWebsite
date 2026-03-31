import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { getMustChangePassword, isAuthenticated } from '../../features/admin/auth/adminAuth';

/** Allows only the forced password-change flow while the session flag is set. */
export function ProtectedPasswordChangeRoute() {
  const location = useLocation();

  if (!isAuthenticated()) {
    return <Navigate to="/lord/login" replace state={{ from: location.pathname }} />;
  }

  if (!getMustChangePassword()) {
    return <Navigate to="/lord/products" replace />;
  }

  return <Outlet />;
}
