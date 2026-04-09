import type { ReactNode } from 'react';
import { Alert, Box, Button, Card, Container, Skeleton, Stack, Typography } from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Link as RouterLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchFeaturedProducts } from '../api/homeApi';
import { FeaturedProductCard } from './FeaturedProductCard';

const FEATURED_LIMIT = 8;
const SKELETON_COUNT = FEATURED_LIMIT;

const GRID_SX = {
  display: 'grid',
  gridTemplateColumns: {
    xs: '1fr',
    sm: 'repeat(2, minmax(0, 1fr))',
    md: 'repeat(3, minmax(0, 1fr))',
    lg: 'repeat(4, minmax(0, 1fr))',
  },
  gap: { xs: 2, md: 3 },
} as const;

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

interface SectionShellProps {
  children: ReactNode;
  showViewAll?: boolean;
  subtitle?: string;
}

function SectionShell({ children, showViewAll = false, subtitle }: SectionShellProps) {
  return (
    <Box
      component="section"
      aria-label="Featured products"
      sx={{ py: { xs: 6, md: 9 }, bgcolor: '#fff' }}
    >
      <Container maxWidth="lg">
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'flex-end' }}
          spacing={1}
          sx={{ mb: { xs: 3, md: 5 } }}
        >
          <Box>
            <Box aria-hidden sx={{ width: 32, height: '2px', bgcolor: 'primary.main', mb: 1.5 }} />
            <Typography
              variant="h4"
              component="h2"
              fontWeight={800}
              sx={{ fontSize: { xs: '1.5rem', md: '1.85rem' } }}
            >
              Trending Now
            </Typography>
            {subtitle ? (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                {subtitle}
              </Typography>
            ) : null}
          </Box>

          {showViewAll && (
            <Button
              component={RouterLink}
              to="/products"
              endIcon={<ArrowForwardIcon />}
              sx={{ fontWeight: 600, display: { xs: 'none', sm: 'inline-flex' } }}
            >
              View All Products
            </Button>
          )}
        </Stack>

        {children}
      </Container>
    </Box>
  );
}

export function FeaturedProductsSection() {
  const { data: products, status, refetch, isFetching } = useQuery({
    queryKey: ['featured-products'],
    queryFn: () => fetchFeaturedProducts(FEATURED_LIMIT),
    staleTime: 5 * 60 * 1000,
  });

  // 1. First load, no cached data yet — show skeleton grid in a stable shell.
  if (status === 'pending') {
    return (
      <SectionShell>
        <Box sx={GRID_SX} aria-busy="true" aria-live="polite">
          {Array.from({ length: SKELETON_COUNT }).map((_, i) => (
            <Box key={i}>
              <CardSkeleton />
            </Box>
          ))}
        </Box>
      </SectionShell>
    );
  }

  // 2. Error — distinct branch, retry affordance.
  if (status === 'error') {
    return (
      <SectionShell>
        <Alert
          severity="warning"
          action={
            <Button color="inherit" size="small" onClick={() => refetch()} disabled={isFetching}>
              {isFetching ? 'Retrying…' : 'Retry'}
            </Button>
          }
        >
          Failed to load featured products. Please try again.
        </Alert>
      </SectionShell>
    );
  }

  // 3. Success but empty — hide the section entirely (no header flash).
  if (!products || products.length === 0) {
    return null;
  }

  // 4. Success with data.
  return (
    <SectionShell showViewAll subtitle="Our latest finds, ready for a new home">
      <Box sx={GRID_SX}>
        {products.map((product) => (
          <Box key={product.id}>
            <FeaturedProductCard product={product} />
          </Box>
        ))}
      </Box>

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
    </SectionShell>
  );
}
