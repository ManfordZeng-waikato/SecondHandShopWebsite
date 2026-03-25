import { Box, Button, Container, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Link as RouterLink } from 'react-router-dom';

export function HomeHeroSection() {
  return (
    <Box
      component="section"
      aria-label="Hero"
      sx={{ bgcolor: '#f0ebe4', overflow: 'hidden' }}
    >
      <Container maxWidth="lg">
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          alignItems="center"
          spacing={{ xs: 4, md: 6 }}
          sx={{ py: { xs: 6, md: 10 } }}
        >
          {/* Illustration — shown first on mobile for visual impact */}
          <Box
            sx={{
              flex: { md: '1 1 55%' },
              width: '100%',
              maxWidth: { xs: 400, md: 'none' },
              order: { xs: 1, md: 1 },
            }}
          >
            <Box
              component="img"
              src="/9987_Pats_shed.svg"
              alt="Pat's Shed — quality second-hand furniture"
              sx={{
                display: 'block',
                width: '100%',
                height: 'auto',
                maxHeight: { xs: 280, sm: 340, md: 420 },
                objectFit: 'contain',
                borderRadius: 4,
              }}
            />
          </Box>

          {/* Text */}
          <Box
            sx={{
              flex: { md: '1 1 45%' },
              textAlign: { xs: 'center', md: 'left' },
              order: { xs: 2, md: 2 },
            }}
          >
            <Typography
              variant="body1"
              color="text.secondary"
              fontWeight={500}
              sx={{ mb: 1, letterSpacing: 1.5, textTransform: 'uppercase', fontSize: '0.8rem' }}
            >
              Welcome to
            </Typography>

            <Typography
              variant="h2"
              component="h1"
              fontWeight={800}
              sx={{
                fontSize: { xs: '2.2rem', sm: '2.8rem', md: '3.2rem' },
                lineHeight: 1.15,
                mb: 2,
              }}
            >
              Pat's Shed
            </Typography>

            <Typography
              variant="h6"
              component="p"
              color="text.secondary"
              fontWeight={400}
              sx={{
                fontSize: { xs: '1rem', md: '1.15rem' },
                lineHeight: 1.6,
                maxWidth: 440,
                mx: { xs: 'auto', md: 0 },
                mb: 4,
              }}
            >
              Quality Second Hand Furniture &amp; Antiques.
            </Typography>

            <Button
              variant="contained"
              size="large"
              endIcon={<ArrowForwardIcon />}
              component={RouterLink}
              to="/products"
              sx={{
                px: 4,
                py: 1.5,
                borderRadius: 2,
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '1rem',
              }}
            >
              Browse Products
            </Button>
          </Box>
        </Stack>
      </Container>
    </Box>
  );
}
