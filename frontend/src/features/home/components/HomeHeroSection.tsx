import { Box, Button, Container, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Link as RouterLink } from 'react-router-dom';

export function HomeHeroSection() {
  return (
    <Box
      component="section"
      aria-label="Hero"
      sx={{
        bgcolor: '#f0ebe4',
        overflow: 'hidden',
        position: 'relative',
      }}
    >
      {/* Subtle dot-grid texture */}
      <Box
        aria-hidden
        sx={{
          position: 'absolute',
          inset: 0,
          backgroundImage: 'radial-gradient(circle, rgba(0,0,0,0.05) 1px, transparent 1px)',
          backgroundSize: '22px 22px',
          pointerEvents: 'none',
        }}
      />

      <Container maxWidth="lg" sx={{ position: 'relative' }}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          alignItems="center"
          spacing={{ xs: 4, md: 8 }}
          sx={{ py: { xs: 7, md: 11 } }}
        >
          {/* Illustration */}
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
              src="/LogoHome.svg"
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

          {/* Copy */}
          <Box
            sx={{
              flex: { md: '1 1 45%' },
              textAlign: { xs: 'center', md: 'left' },
              order: { xs: 2, md: 2 },
            }}
          >
            {/* Eyebrow */}
            <Typography
              component="span"
              sx={{
                display: 'inline-block',
                mb: 2,
                letterSpacing: '0.14em',
                textTransform: 'uppercase',
                fontSize: '0.72rem',
                fontWeight: 600,
                color: 'text.secondary',
                borderBottom: '1.5px solid',
                borderColor: 'text.secondary',
                pb: '3px',
              }}
            >
              Hamilton, New Zealand
            </Typography>

            <Typography
              variant="h2"
              component="h1"
              sx={{
                fontSize: { xs: '2.8rem', sm: '3.4rem', md: '4rem' },
                lineHeight: 1.08,
                mb: 2,
              }}
            >
              Pat's Shed
            </Typography>

            <Typography
              variant="body1"
              color="text.secondary"
              sx={{
                fontSize: { xs: '1rem', md: '1.15rem' },
                lineHeight: 1.7,
                maxWidth: 440,
                mx: { xs: 'auto', md: 0 },
                mb: 4,
              }}
            >
              Quality pre-loved furniture &amp; antiques, hand-picked from
              the&nbsp;Waikato. Come find your next favourite piece.
            </Typography>

            {/* Accent rule */}
            <Box
              aria-hidden
              sx={{
                width: 44,
                height: '2px',
                bgcolor: 'primary.main',
                mb: 4,
                mx: { xs: 'auto', md: 0 },
              }}
            />

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
