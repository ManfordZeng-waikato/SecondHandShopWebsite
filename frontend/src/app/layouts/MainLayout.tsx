import type { PropsWithChildren } from 'react';
import { AppBar, Box, Button, Container, Link, Stack, Toolbar, Typography } from '@mui/material';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import FacebookIcon from '@mui/icons-material/Facebook';
import InstagramIcon from '@mui/icons-material/Instagram';
import { Link as RouterLink } from 'react-router-dom';

interface MainLayoutProps extends PropsWithChildren {
  fullWidth?: boolean;
}

export function MainLayout({ children, fullWidth }: MainLayoutProps) {
  return (
    <Box minHeight="100vh" display="flex" flexDirection="column">
      <AppBar position="static">
        <Toolbar>
          <Typography
            variant="h6"
            component={RouterLink}
            to="/"
            sx={{ flexGrow: 1, textDecoration: 'none', color: 'inherit' }}
          >
            Pat's Shed
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button color="inherit" component={RouterLink} to="/products">
              Products
            </Button>
            <Button color="inherit" component={RouterLink} to="/my-story">
              My Story
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>
      {fullWidth ? (
        <Box sx={{ flexGrow: 1 }}>{children}</Box>
      ) : (
        <Container sx={{ py: 4, flexGrow: 1 }}>{children}</Container>
      )}
      <Box component="footer" sx={{ borderTop: '1px solid', borderColor: 'divider', py: 3, bgcolor: 'grey.100' }}>
        <Container>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={{ xs: 3, sm: 2 }}
            justifyContent="space-between"
            alignItems={{ xs: 'center', sm: 'flex-start' }}
          >
            <Link
              href="https://www.google.com/maps?q=97+Kay+Road+RD1+Hamilton+3281+New+Zealand"
              target="_blank"
              rel="noopener noreferrer"
              underline="hover"
              color="text.primary"
              sx={{ whiteSpace: 'pre-line', textAlign: { xs: 'center', sm: 'left' }, fontSize: '0.95rem', fontWeight: 500 }}
            >
              {'Address\n97 Kay Road\nRD1\nHamilton 3281\nNew Zealand'}
            </Link>

            <Stack spacing={1} alignItems="center" sx={{ order: { xs: -1, sm: 0 } }}>
              <Box
                component="img"
                src="/logo.svg"
                alt="Pat's Shed logo"
                sx={{ width: 100, height: 'auto' }}
              />
              <Typography
                variant="body1"
                color="text.primary"
                fontWeight={500}
                sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75 }}
              >
                <AccessTimeIcon fontSize="small" />
                Open Daily 9am – 5pm
              </Typography>
            </Stack>

            <Stack spacing={1} alignItems={{ xs: 'center', sm: 'flex-start' }}>
              <Link
                href="https://www.facebook.com/pat.gsq"
                target="_blank"
                rel="noopener noreferrer"
                underline="hover"
                color="text.primary"
                sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75, fontSize: '0.95rem', fontWeight: 500 }}
              >
                <FacebookIcon fontSize="small" />
                Facebook
              </Link>
              <Link
                href="https://www.instagram.com/patsshed?igsh=NGJ4eDJkNnliemJh"
                target="_blank"
                rel="noopener noreferrer"
                underline="hover"
                color="text.primary"
                sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75, fontSize: '0.95rem', fontWeight: 500 }}
              >
                <InstagramIcon fontSize="small" />
                Instagram
              </Link>
            </Stack>
          </Stack>
        </Container>
      </Box>
    </Box>
  );
}
