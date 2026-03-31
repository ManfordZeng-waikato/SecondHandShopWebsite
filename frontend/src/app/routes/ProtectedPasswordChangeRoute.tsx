import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { adminRequiresPasswordChange, isAdminLoggedIn } from '../../features/admin/auth/adminSession';

/** Allows only the forced password-change flow while the session flag is set. */
export function ProtectedPasswordChangeRoute() {
  const location = useLocation();

  if (!isAdminLoggedIn()) {
    return <Navigate to="/lord/login" replace state={{ from: location.pathname }} />;
  }

  if (!adminRequiresPasswordChange()) {
    return <Navigate to="/lord/products" replace />;
  }

  return <Outlet />;
}
