import type { PropsWithChildren } from 'react';
import { AppBar, Box, Button, Container, Link, Stack, Toolbar, Typography } from '@mui/material';
import FacebookIcon from '@mui/icons-material/Facebook';
import { Link as RouterLink } from 'react-router-dom';

export function MainLayout({ children }: PropsWithChildren) {
  return (
    <Box minHeight="100vh" display="flex" flexDirection="column">
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Pat's Shed
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button color="inherit" component={RouterLink} to="/">
              Product List
            </Button>
            <Button color="inherit" component={RouterLink} to="/my-story">
              My Story
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>
      <Container sx={{ py: 4, flexGrow: 1 }}>{children}</Container>
      <Box component="footer" sx={{ borderTop: '1px solid', borderColor: 'divider', py: 3, bgcolor: 'grey.100' }}>
        <Container>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between">
            <Link
              href="https://www.google.com/maps?q=97+Kay+Road+RD1+Hamilton+3281+New+Zealand"
              target="_blank"
              rel="noopener noreferrer"
              underline="hover"
              color="text.secondary"
              sx={{ whiteSpace: 'pre-line' }}
            >
              {'Address\n97 Kay Road\nRD1\nHamilton 3281\nNew Zealand'}
            </Link>
            <Link
              href="https://www.facebook.com/pat.gsq"
              target="_blank"
              rel="noopener noreferrer"
              underline="hover"
              color="text.secondary"
              sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75 }}
            >
              <FacebookIcon fontSize="small" />
              Facebook
            </Link>
          </Stack>
        </Container>
      </Box>
    </Box>
  );
}
