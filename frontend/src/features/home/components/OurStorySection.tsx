import { Box, Button, Container, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Link as RouterLink } from 'react-router-dom';

export function OurStorySection() {
  return (
    <Box
      component="section"
      aria-label="Our story"
      sx={{
        bgcolor: '#f0ebe4',
        py: { xs: 7, md: 11 },
        position: 'relative',
        overflow: 'hidden',
        backgroundImage: 'url(/Title.svg)',
        backgroundRepeat: 'no-repeat',
        backgroundPosition: 'center',
        backgroundSize: '90%',
      }}
    >

      <Container maxWidth="sm" sx={{ position: 'relative' }}>
        <Box sx={{ textAlign: 'center' }}>
          {/* Decorative opening mark */}
          <Box
            aria-hidden
            sx={{
              fontFamily: "'Cormorant Garamond', Georgia, serif",
              fontSize: { xs: '5rem', md: '7rem' },
              lineHeight: 0.6,
              color: 'primary.main',
              opacity: 0.25,
              mb: 2,
              userSelect: 'none',
            }}
          >
            &ldquo;
          </Box>

          <Typography
            variant="h4"
            component="h2"
            sx={{ fontSize: { xs: '1.5rem', md: '1.85rem' }, mb: 3 }}
          >
            Our Story
          </Typography>

          <Typography
            variant="body1"
            color="text.secondary"
            sx={{ lineHeight: 1.8, mb: 2 }}
          >
            Hi, I'm Pat Jackson. What started as a hobby — finding quality
            pre-loved furniture around the Waikato — has grown into a small family
            business run from my very own shed.
          </Typography>

          <Typography
            variant="body1"
            color="text.secondary"
            sx={{ lineHeight: 1.8, mb: 4 }}
          >
            After 25&nbsp;years of teaching, I retired and turned my passion into
            Pat's Shed. Together with my husband and son, we hand-pick every piece
            to make sure it's ready for its next home. Drop by — we'd love to show
            you around.
          </Typography>

          <Button
            variant="outlined"
            component={RouterLink}
            to="/my-story"
            endIcon={<ArrowForwardIcon />}
            sx={{
              fontWeight: 600,
              borderRadius: 2,
              px: 3,
              py: 1,
            }}
          >
            Read More
          </Button>
        </Box>
      </Container>
    </Box>
  );
}
