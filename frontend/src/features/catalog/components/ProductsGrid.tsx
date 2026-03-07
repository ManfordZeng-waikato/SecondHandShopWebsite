import { memo } from 'react';
import { Grid } from '@mui/material';
import type { ProductListItem } from '../../../entities/product/types';
import { ProductCard } from './ProductCard';

interface ProductsGridProps {
  items: ProductListItem[];
}

export const ProductsGrid = memo(function ProductsGrid({
  items,
}: ProductsGridProps) {
  return (
    <Grid container spacing={2}>
      {items.map((item) => (
        <Grid key={item.id} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
          <ProductCard product={item} />
        </Grid>
      ))}
    </Grid>
  );
});
