import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, CircularProgress, Grid, Stack, Typography } from '@mui/material';
import { fetchCategories, fetchProducts } from '../features/catalog/api/catalogApi';
import { CategorySidebar } from '../features/catalog/components/CategorySidebar';
import { ProductCard } from '../features/catalog/components/ProductCard';

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

  if (categoriesQuery.isLoading || productsQuery.isLoading) {
    return <CircularProgress />;
  }

  if (categoriesQuery.isError || productsQuery.isError) {
    return <Alert severity="error">Unable to load catalog data.</Alert>;
  }

  const categories = categoriesQuery.data ?? [];
  const products = productsQuery.data ?? [];

  return (
    <Stack spacing={3}>
      <Typography variant="h4">Product List</Typography>
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 3 }}>
          <CategorySidebar
            categories={categories}
            selectedCategoryId={selectedCategoryId}
            onSelect={setSelectedCategoryId}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 9 }}>
          <Grid container spacing={2}>
            {products.map((product) => (
              <Grid key={product.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                <ProductCard product={product} />
              </Grid>
            ))}
          </Grid>
        </Grid>
      </Grid>
    </Stack>
  );
}
