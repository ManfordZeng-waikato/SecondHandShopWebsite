import { type FormEvent, useEffect, useState } from 'react';
import { Alert, Box, Button, CircularProgress, Paper, Stack, TextField, Typography } from '@mui/material';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  getAdminAuthSnapshot,
  initializeAdminAuth,
  persistSessionAfterLogin,
  revokeLordCookie,
  useAdminAuth,
} from '../features/admin/auth/adminAuth';
import { loginAdmin } from '../features/admin/api/adminApi';
import axios from 'axios';

export function AdminLoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthInitialized, isAuthenticated, mustChangePassword } = useAdminAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const routeState = location.state as { from?: string; passwordChanged?: boolean } | null;
  const redirectPath = routeState?.from ?? '/lord/products';
  const passwordChangedNotice = routeState?.passwordChanged === true;

  useEffect(() => {
    if (!isAuthInitialized) return;
    if (isAuthenticated) {
      if (mustChangePassword) {
        navigate('/lord/change-password', { replace: true });
      } else {
        navigate(redirectPath, { replace: true });
      }
      return;
    }
    void revokeLordCookie();
  }, [isAuthInitialized, isAuthenticated, mustChangePassword, navigate, redirectPath]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    if (!username.trim() || !password.trim()) {
      setError('Please enter both username and password.');
      return;
    }

    setLoading(true);
    try {
      const { expiresAt } = await loginAdmin(username, password);
      persistSessionAfterLogin(expiresAt);
      await initializeAdminAuth();
      const s = getAdminAuthSnapshot();
      if (s.mustChangePassword) {
        navigate('/lord/change-password', { replace: true });
      } else {
        navigate(redirectPath, { replace: true });
      }
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        setError('Invalid username or password.');
      } else {
        setError('Login failed. Please try again later.');
      }
    } finally {
      setLoading(false);
    }
  };

  if (!isAuthInitialized) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box display="flex" justifyContent="center">
      <Paper sx={{ p: 3, width: 420 }}>
        <Stack spacing={2} component="form" onSubmit={handleSubmit}>
          <Typography variant="h5">Admin login</Typography>
          {passwordChangedNotice && (
            <Alert severity="success">Password changed successfully. Please sign in again.</Alert>
          )}
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label="Username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            disabled={loading}
          />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            disabled={loading}
          />
          <Button type="submit" variant="contained" disabled={loading}>
            {loading ? <CircularProgress size={24} /> : 'Sign in'}
          </Button>
        </Stack>
      </Paper>
    </Box>
  );
}
