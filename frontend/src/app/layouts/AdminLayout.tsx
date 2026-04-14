import type { PropsWithChildren } from 'react';
import { AppBar, Box, Button, Container, Divider, Stack, Toolbar, Tooltip, Typography } from '@mui/material';
import LogoutIcon from '@mui/icons-material/Logout';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { clearAuth } from '../../features/admin/auth/adminAuth';
import { logoutAdmin } from '../../features/admin/api/adminApi';

export function AdminLayout({ children }: PropsWithChildren) {
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logoutAdmin();
    } finally {
      clearAuth();
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
          <Stack direction="row" spacing={1} alignItems="center">
            <Button color="inherit" component={RouterLink} to="/lord/products">
              Products
            </Button>
            <Button color="inherit" component={RouterLink} to="/lord/customers">
              Customers
            </Button>
            <Button color="inherit" component={RouterLink} to="/lord/analytics">
              Analytics
            </Button>
            <Tooltip title="Open public site in a new tab">
              <Button
                color="inherit"
                component="a"
                href="/"
                target="_blank"
                rel="noopener noreferrer"
                endIcon={<OpenInNewIcon sx={{ fontSize: 16 }} />}
              >
                View site
              </Button>
            </Tooltip>
            <Divider
              orientation="vertical"
              flexItem
              sx={{ mx: 0.5, my: 1.25, borderColor: 'rgba(255,255,255,0.24)' }}
            />
            <Button
              onClick={handleLogout}
              startIcon={<LogoutIcon sx={{ fontSize: 18 }} />}
              variant="outlined"
              size="small"
              sx={{
                color: '#ff8a80',
                borderColor: 'rgba(255,138,128,0.5)',
                fontWeight: 600,
                letterSpacing: '0.02em',
                '&:hover': {
                  borderColor: '#ff5252',
                  backgroundColor: 'rgba(255,82,82,0.12)',
                  color: '#ff5252',
                },
              }}
            >
              Logout
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>
      <Container sx={{ py: 4 }}>{children}</Container>
    </Box>
  );
}
