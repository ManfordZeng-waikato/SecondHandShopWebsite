import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Card,
  Grid,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import StorefrontIcon from '@mui/icons-material/Storefront';
import SearchOffIcon from '@mui/icons-material/SearchOff';
import { fetchCategories, fetchProducts } from '../features/catalog/api/catalogApi';
import { CategorySidebar } from '../features/catalog/components/CategorySidebar';
import { ProductCard } from '../features/catalog/components/ProductCard';

function ProductSkeleton() {
  return (
    <Card sx={{ borderRadius: 3, overflow: 'hidden' }}>
      <Skeleton variant="rectangular" height={220} animation="wave" />
      <Box sx={{ p: 2 }}>
        <Skeleton variant="text" width="70%" height={28} />
        <Skeleton variant="text" width="100%" height={18} />
        <Skeleton variant="text" width="60%" height={18} />
        <Skeleton variant="text" width="40%" height={32} sx={{ mt: 1 }} />
      </Box>
    </Card>
  );
}

export function HomePage() {
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: fetchCategories,
  });

  const productsQuery = useQuery({
    queryKey: ['products', selectedCategoryId],
    queryFn: () => fetchProducts(selectedCategoryId),
  });

  const isLoading = categoriesQuery.isLoading || productsQuery.isLoading;

  if (categoriesQuery.isError || productsQuery.isError) {
    return <Alert severity="error">Unable to load catalog data.</Alert>;
  }

  const categories = categoriesQuery.data ?? [];
  const products = productsQuery.data ?? [];

  return (
    <Stack spacing={4}>
      {/* Page header */}
      <Box>
        <Stack direction="row" alignItems="center" spacing={1.5} mb={0.5}>
          <StorefrontIcon sx={{ fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" fontWeight={800}>
            Discover Products
          </Typography>
        </Stack>
        <Typography variant="body1" color="text.secondary">
          Browse our curated selection of quality second-hand items
        </Typography>
      </Box>

      <Grid container spacing={3}>
        {/* Sidebar */}
        <Grid size={{ xs: 12, md: 3 }}>
          {isLoading ? (
            <Skeleton variant="rounded" height={160} sx={{ borderRadius: 3 }} animation="wave" />
          ) : (
            <CategorySidebar
              categories={categories}
              selectedCategoryId={selectedCategoryId}
              onSelect={setSelectedCategoryId}
            />
          )}
        </Grid>

        {/* Product grid */}
        <Grid size={{ xs: 12, md: 9 }}>
          {isLoading ? (
            <Grid container spacing={2}>
              {Array.from({ length: 6 }).map((_, i) => (
                <Grid key={i} size={{ xs: 12, sm: 6, lg: 4 }}>
                  <ProductSkeleton />
                </Grid>
              ))}
            </Grid>
          ) : products.length === 0 ? (
            <Stack
              alignItems="center"
              justifyContent="center"
              spacing={2}
              sx={{ py: 10, color: 'text.secondary' }}
            >
              <SearchOffIcon sx={{ fontSize: 64, opacity: 0.4 }} />
              <Typography variant="h6" fontWeight={600}>
                No products found
              </Typography>
              <Typography variant="body2">
                Try selecting a different category or check back later.
              </Typography>
            </Stack>
          ) : (
            <>
              <Typography variant="body2" color="text.secondary" mb={2}>
                {products.length} {products.length === 1 ? 'item' : 'items'} found
              </Typography>
              <Grid container spacing={2}>
                {products.map((product) => (
                  <Grid key={product.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                    <ProductCard product={product} />
                  </Grid>
                ))}
              </Grid>
            </>
          )}
        </Grid>
      </Grid>
    </Stack>
  );
}
