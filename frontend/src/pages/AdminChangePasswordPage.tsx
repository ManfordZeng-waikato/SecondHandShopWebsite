import { type FormEvent, useState } from 'react';
import { Alert, Box, Button, CircularProgress, Paper, Stack, TextField, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { changeAdminInitialPassword } from '../features/admin/api/adminApi';
import { saveAdminSession } from '../features/admin/auth/adminSession';

export function AdminChangePasswordPage() {
  const navigate = useNavigate();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    if (!currentPassword || !newPassword || !confirmNewPassword) {
      setError('All fields are required.');
      return;
    }

    setLoading(true);
    try {
      const result = await changeAdminInitialPassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      });
      saveAdminSession(result.expiresAt, { requiresPasswordChange: result.requiresPasswordChange });
      navigate('/lord/products', { replace: true });
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        setError('Current password is incorrect.');
      } else if (axios.isAxiosError(err) && err.response?.status === 400) {
        const msg = (err.response.data as { message?: string })?.message;
        setError(typeof msg === 'string' ? msg : 'Invalid input. Check password rules and confirmation.');
      } else if (axios.isAxiosError(err) && err.response?.status === 409) {
        const msg = (err.response.data as { message?: string })?.message;
        setError(typeof msg === 'string' ? msg : 'This action is not available. Try signing in again.');
      } else {
        setError('Could not update password. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box display="flex" justifyContent="center">
      <Paper sx={{ p: 3, width: 440 }}>
        <Stack spacing={2} component="form" onSubmit={handleSubmit}>
          <Typography variant="h5">Set a new password</Typography>
          <Typography variant="body2" color="text.secondary">
            Your account must use a new password before accessing the admin dashboard. Use at least 8 characters
            with at least one letter and one digit.
          </Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label="Current password"
            type="password"
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
            disabled={loading}
            autoComplete="current-password"
          />
          <TextField
            label="New password"
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            disabled={loading}
            autoComplete="new-password"
          />
          <TextField
            label="Confirm new password"
            type="password"
            value={confirmNewPassword}
            onChange={(event) => setConfirmNewPassword(event.target.value)}
            disabled={loading}
            autoComplete="new-password"
          />
          <Button type="submit" variant="contained" disabled={loading}>
            {loading ? <CircularProgress size={24} /> : 'Update password'}
          </Button>
        </Stack>
      </Paper>
    </Box>
  );
}
