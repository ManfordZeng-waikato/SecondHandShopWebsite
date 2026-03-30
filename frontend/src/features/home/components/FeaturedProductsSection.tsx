import { Alert, Box, Button, Card, Container, Skeleton, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Link as RouterLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchFeaturedProducts } from '../api/homeApi';
import { FeaturedProductCard } from './FeaturedProductCard';

const FEATURED_LIMIT = 8;
const SKELETON_COUNT = 4;

function CardSkeleton() {
  return (
    <Card sx={{ borderRadius: 3, overflow: 'hidden' }}>
      <Skeleton variant="rectangular" sx={{ paddingTop: '75%' }} animation="wave" />
      <Box sx={{ p: 2 }}>
        <Skeleton variant="text" width="80%" height={22} />
        <Skeleton variant="text" width="50%" height={28} sx={{ mt: 0.75 }} />
      </Box>
    </Card>
  );
}

export function FeaturedProductsSection() {
  const {
    data: products,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['featured-products'],
    queryFn: () => fetchFeaturedProducts(FEATURED_LIMIT),
    staleTime: 5 * 60 * 1000,
  });

  const hasProducts = !isLoading && products && products.length > 0;

  if (isError) {
    return (
      <Box component="section" aria-label="Featured products" sx={{ py: { xs: 6, md: 8 }, bgcolor: '#fff' }}>
        <Container maxWidth="lg">
          <Stack spacing={2}>
            <Typography variant="h4" component="h2" fontWeight={800}>
              Trending Now
            </Typography>
            <Alert
              severity="warning"
              action={
                <Button color="inherit" size="small" onClick={() => refetch()}>
                  Retry
                </Button>
              }
            >
              Failed to load featured products. Please try again.
            </Alert>
          </Stack>
        </Container>
      </Box>
    );
  }

  if (!isLoading && (!products || products.length === 0)) return null;

  return (
    <Box
      component="section"
      aria-label="Featured products"
      sx={{ py: { xs: 6, md: 9 }, bgcolor: '#fff' }}
    >
      <Container maxWidth="lg">
        {/* Section header */}
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'flex-end' }}
          spacing={1}
          sx={{ mb: { xs: 3, md: 5 } }}
        >
          <Box>
            {/* Accent rule */}
            <Box
              aria-hidden
              sx={{ width: 32, height: '2px', bgcolor: 'primary.main', mb: 1.5 }}
            />
            <Typography
              variant="h4"
              component="h2"
              fontWeight={800}
              sx={{ fontSize: { xs: '1.5rem', md: '1.85rem' } }}
            >
              Trending Now
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              Our latest finds, ready for a new home
            </Typography>
          </Box>

          {hasProducts && (
            <Button
              component={RouterLink}
              to="/products"
              endIcon={<ArrowForwardIcon />}
              sx={{
                fontWeight: 600,
                display: { xs: 'none', sm: 'inline-flex' },
              }}
            >
              View All Products
            </Button>
          )}
        </Stack>

        {/* Product cards */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, minmax(0, 1fr))',
              md: 'repeat(3, minmax(0, 1fr))',
              lg: 'repeat(4, minmax(0, 1fr))',
            },
            gap: { xs: 2, md: 3 },
          }}
        >
          {isLoading
            ? Array.from({ length: SKELETON_COUNT }).map((_, i) => (
                <Box key={i}>
                  <CardSkeleton />
                </Box>
              ))
            : products?.map((product) => (
                <Box key={product.id}>
                  <FeaturedProductCard product={product} />
                </Box>
              ))}
        </Box>

        {/* Mobile "View All" button */}
        {hasProducts && (
          <Box sx={{ display: { xs: 'flex', sm: 'none' }, justifyContent: 'center', mt: 3 }}>
            <Button
              variant="outlined"
              component={RouterLink}
              to="/products"
              endIcon={<ArrowForwardIcon />}
              sx={{ fontWeight: 600, borderRadius: 2 }}
            >
              View All Products
            </Button>
          </Box>
        )}
      </Container>
    </Box>
  );
}
