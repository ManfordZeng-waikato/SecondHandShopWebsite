import { Box, Container, Link, Stack, Typography } from '@mui/material';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import FacebookIcon from '@mui/icons-material/Facebook';
import InstagramIcon from '@mui/icons-material/Instagram';
import PhoneIcon from '@mui/icons-material/Phone';

export function MainFooter() {
  return (
    <Box
      component="footer"
      sx={{
        borderTop: '1px solid',
        borderColor: 'primary.dark',
        py: 3,
        bgcolor: 'primary.main',
        color: 'primary.contrastText',
      }}
    >
      <Container>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={{ xs: 3, sm: 2 }}
          justifyContent="space-between"
          alignItems={{ xs: 'center', sm: 'flex-start' }}
        >
          {/* Left — Address */}
          <Link
            href="https://www.google.com/maps?q=97+Kay+Road+RD1+Hamilton+3281+New+Zealand"
            target="_blank"
            rel="noopener noreferrer"
            underline="hover"
            color="inherit"
            sx={{ whiteSpace: 'pre-line', textAlign: { xs: 'center', sm: 'left' }, fontSize: '0.95rem', fontWeight: 500 }}
          >
            {'Address\n97 Kay Road\nRD1\nHamilton 3281\nNew Zealand'}
          </Link>

          {/* Centre — Logo + Hours */}
          <Stack spacing={1} alignItems="center" sx={{ order: { xs: -1, sm: 0 } }}>
            <Box
              component="img"
              src="/Title.svg"
              alt="Pat's Shed logo"
              sx={{ width: 100, height: 'auto' }}
            />
            <Typography
              variant="body1"
              color="inherit"
              fontWeight={500}
              sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75 }}
            >
              <AccessTimeIcon fontSize="small" />
              Open Daily 9am - 5pm
            </Typography>
            <Typography
              variant="caption"
              sx={{
                display: { xs: 'none', sm: 'block' },
                color: 'rgba(255,255,255,0.35)',
                fontSize: '0.72rem',
                letterSpacing: '0.04em',
                mt: 1,
              }}
            >
              &copy; {new Date().getFullYear()} Pat&apos;s Shed. All rights reserved.
            </Typography>
          </Stack>

          {/* Right — Social / Contact */}
          <Stack spacing={1} alignItems={{ xs: 'center', sm: 'flex-start' }}>
            <Link
              href="https://www.facebook.com/pat.gsq"
              target="_blank"
              rel="noopener noreferrer"
              underline="hover"
              color="inherit"
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
              color="inherit"
              sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75, fontSize: '0.95rem', fontWeight: 500 }}
            >
              <InstagramIcon fontSize="small" />
              Instagram
            </Link>
            <Link
              href="tel:+6421735836"
              underline="hover"
              color="inherit"
              sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.75, fontSize: '0.95rem', fontWeight: 500 }}
            >
              <PhoneIcon fontSize="small" />
              021 735 836
            </Link>
          </Stack>
        </Stack>

        <Typography
          variant="caption"
          sx={{
            display: { xs: 'block', sm: 'none' },
            color: 'rgba(255,255,255,0.35)',
            fontSize: '0.72rem',
            letterSpacing: '0.04em',
            textAlign: 'center',
            mt: 3,
          }}
        >
          &copy; {new Date().getFullYear()} Pat&apos;s Shed. All rights reserved.
        </Typography>
      </Container>
    </Box>
  );
}
