import { Box, CircularProgress } from '@mui/material';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAdminAuth } from '../../features/admin/auth/adminAuth';

function AuthBootstrapSpinner() {
  return (
    <Box display="flex" justifyContent="center" alignItems="center" minHeight="100vh">
      <CircularProgress />
    </Box>
  );
}

export function ProtectedAdminRoute() {
  const location = useLocation();
  const { isAuthInitialized, isAuthenticated, mustChangePassword } = useAdminAuth();

  if (!isAuthInitialized) {
    return <AuthBootstrapSpinner />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/lord/login" replace state={{ from: location.pathname }} />;
  }

  if (mustChangePassword) {
    return <Navigate to="/lord/change-password" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
