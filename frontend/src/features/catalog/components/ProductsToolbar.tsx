import { Paper, Stack } from '@mui/material';
import type { Category } from '../../../entities/category/types';
import type {
  ProductQueryParams,
  ProductSortOption,
} from '../../../entities/product/types';
import { ProductFilters } from './ProductFilters';
import { ProductSortSelect } from './ProductSortSelect';

interface ProductsToolbarProps {
  params: ProductQueryParams;
  categories: Category[];
  onFilterChange: (
    updates: Partial<ProductQueryParams>,
    resetPage?: boolean,
  ) => void;
}

export function ProductsToolbar({
  params,
  categories,
  onFilterChange,
}: ProductsToolbarProps) {
  return (
    <Paper
      sx={{
        p: 2.5,
        borderRadius: 3,
        bgcolor: '#f0ebe4',
        backgroundImage: 'url(/Title.svg)',
        backgroundRepeat: 'no-repeat',
        backgroundPosition: 'center',
        backgroundSize: '90%',
      }}
    >
      <Stack spacing={2}>
        <ProductFilters
          params={params}
          categories={categories}
          onFilterChange={onFilterChange}
        />
        <Stack
          direction="row"
          justifyContent="flex-end"
          alignItems="center"
        >
          <ProductSortSelect
            value={params.sort}
            onChange={(sort: ProductSortOption | undefined) =>
              onFilterChange({ sort })
            }
          />
        </Stack>
      </Stack>
    </Paper>
  );
}
