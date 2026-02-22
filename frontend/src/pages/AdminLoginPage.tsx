import { type FormEvent, useState } from 'react';
import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { useLocation, useNavigate } from 'react-router-dom';
import { loginAsAdmin } from '../features/admin/auth/adminSession';

export function AdminLoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const redirectPath = (location.state as { from?: string } | null)?.from ?? '/admin/products';

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!username.trim() || !password.trim()) {
      setError('Please enter both username and password.');
      return;
    }

    loginAsAdmin();
    navigate(redirectPath, { replace: true });
  };

  return (
    <Box display="flex" justifyContent="center">
      <Paper sx={{ p: 3, width: 420 }}>
        <Stack spacing={2} component="form" onSubmit={handleSubmit}>
          <Typography variant="h5">Admin login</Typography>
          <Typography variant="body2" color="text.secondary">
            Placeholder login only. Real authentication will be integrated later.
          </Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label="Username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
          />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <Button type="submit" variant="contained">
            Enter admin dashboard
          </Button>
        </Stack>
      </Paper>
    </Box>
  );
}
