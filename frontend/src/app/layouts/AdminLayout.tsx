import type { PropsWithChildren } from 'react';
import { AppBar, Box, Button, Container, Stack, Toolbar, Typography } from '@mui/material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { clearAdminSession } from '../../features/admin/auth/adminSession';
import { logoutAdmin } from '../../features/admin/api/adminApi';

export function AdminLayout({ children }: PropsWithChildren) {
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logoutAdmin();
    } finally {
      clearAdminSession();
      navigate('/lord/login');
    }
  };

  return (
    <Box minHeight="100vh">
      <AppBar position="static" color="secondary">
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Admin Dashboard
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button color="inherit" component={RouterLink} to="/lord/products">
              Products
            </Button>
            <Button color="inherit" component={RouterLink} to="/lord/products/new">
              New product
            </Button>
            <Button color="inherit" onClick={handleLogout}>
              Logout
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>
      <Container sx={{ py: 4 }}>{children}</Container>
    </Box>
  );
}
