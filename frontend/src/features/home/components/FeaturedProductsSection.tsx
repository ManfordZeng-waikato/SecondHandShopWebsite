import { Box, Button, Card, Container, Skeleton, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Link as RouterLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchFeaturedProducts } from '../api/homeApi';
import { FeaturedProductCard } from './FeaturedProductCard';

const CARD_COUNT = 5;

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
  const { data: products, isLoading } = useQuery({
    queryKey: ['featured-products'],
    queryFn: fetchFeaturedProducts,
    staleTime: 5 * 60 * 1000,
  });

  const hasProducts = !isLoading && products && products.length > 0;

  if (!isLoading && (!products || products.length === 0)) return null;

  return (
    <Box
      component="section"
      aria-label="Featured products"
      sx={{ py: { xs: 6, md: 8 }, bgcolor: '#fff' }}
    >
      <Container maxWidth="lg">
        {/* Section header */}
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'flex-end' }}
          spacing={1}
          sx={{ mb: { xs: 3, md: 4 } }}
        >
          <Box>
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
                textTransform: 'none',
                fontWeight: 600,
                display: { xs: 'none', sm: 'inline-flex' },
              }}
            >
              View All Products
            </Button>
          )}
        </Stack>

        {/* Product cards — horizontal scroll on mobile, grid on desktop */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: `repeat(${CARD_COUNT}, 240px)`,
              lg: `repeat(${CARD_COUNT}, 1fr)`,
            },
            gap: { xs: 2, lg: 3 },
            overflowX: { xs: 'auto', lg: 'hidden' },
            scrollSnapType: { xs: 'x mandatory', lg: 'none' },
            mx: { xs: -2, sm: -3, lg: 0 },
            px: { xs: 2, sm: 3, lg: 0 },
            pb: { xs: 1.5, lg: 0 },
            '&::-webkit-scrollbar': { height: 4 },
            '&::-webkit-scrollbar-track': { bgcolor: 'transparent' },
            '&::-webkit-scrollbar-thumb': {
              bgcolor: '#d9d9d9',
              borderRadius: 2,
            },
            scrollbarWidth: 'thin',
            scrollbarColor: '#d9d9d9 transparent',
          }}
        >
          {isLoading
            ? Array.from({ length: CARD_COUNT }).map((_, i) => (
                <Box key={i} sx={{ scrollSnapAlign: 'start' }}>
                  <CardSkeleton />
                </Box>
              ))
            : products?.map((product) => (
                <Box key={product.id} sx={{ scrollSnapAlign: 'start' }}>
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
              sx={{ textTransform: 'none', fontWeight: 600, borderRadius: 2 }}
            >
              View All Products
            </Button>
          </Box>
        )}
      </Container>
    </Box>
  );
}
