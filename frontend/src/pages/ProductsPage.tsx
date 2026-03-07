import { useCallback, useRef } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Stack,
  Typography,
} from '@mui/material';
import StorefrontIcon from '@mui/icons-material/Storefront';
import SearchOffIcon from '@mui/icons-material/SearchOff';
import RefreshIcon from '@mui/icons-material/Refresh';
import {
  fetchCategories,
  fetchProductsPaged,
} from '../features/catalog/api/catalogApi';
import { useProductFilters } from '../features/catalog/hooks/useProductFilters';
import { ProductsToolbar } from '../features/catalog/components/ProductsToolbar';
import { ProductsGrid } from '../features/catalog/components/ProductsGrid';
import { Pagination } from '../features/catalog/components/Pagination';
import { ProductsSkeleton } from '../features/catalog/components/ProductsSkeleton';

export function ProductsPage() {
  const gridRef = useRef<HTMLDivElement>(null);
  const { params, setFilters, resetFilters } = useProductFilters();

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: fetchCategories,
    staleTime: 5 * 60 * 1000,
  });

  const productsQuery = useQuery({
    queryKey: ['products-paged', params],
    queryFn: ({ signal }) => fetchProductsPaged(params, signal),
    placeholderData: keepPreviousData,
  });

  const handlePageChange = useCallback(
    (page: number) => {
      setFilters({ page }, false);
      gridRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    },
    [setFilters],
  );

  const result = productsQuery.data;
  const isInitialLoading = productsQuery.isLoading;
  const isError = productsQuery.isError;
  const isFetching = productsQuery.isFetching;

  return (
    <Stack spacing={3}>
      {/* Page header */}
      <Box>
        <Stack direction="row" alignItems="center" spacing={1.5} mb={0.5}>
          <StorefrontIcon sx={{ fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" fontWeight={800}>
            All Products
          </Typography>
        </Stack>
        <Typography variant="body1" color="text.secondary">
          Browse our curated collection of quality second-hand furniture
        </Typography>
      </Box>

      {/* Filters toolbar */}
      <ProductsToolbar
        params={params}
        categories={categoriesQuery.data ?? []}
        onFilterChange={setFilters}
      />

      {/* Product grid */}
      <Box
        ref={gridRef}
        sx={{
          minHeight: 400,
          opacity: isFetching && !isInitialLoading ? 0.6 : 1,
          transition: 'opacity 0.2s ease',
        }}
      >
        {isInitialLoading ? (
          <ProductsSkeleton />
        ) : isError ? (
          <Alert
            severity="error"
            action={
              <Button
                color="inherit"
                size="small"
                startIcon={<RefreshIcon />}
                onClick={() => productsQuery.refetch()}
              >
                Retry
              </Button>
            }
          >
            Failed to load products. Please try again.
          </Alert>
        ) : result && result.items.length === 0 ? (
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
              Try adjusting your filters or check back later.
            </Typography>
            <Button variant="outlined" size="small" onClick={resetFilters}>
              Clear all filters
            </Button>
          </Stack>
        ) : result ? (
          <>
            <Typography variant="body2" color="text.secondary" mb={2}>
              {result.totalCount}{' '}
              {result.totalCount === 1 ? 'item' : 'items'} found
            </Typography>

            <ProductsGrid items={result.items} />

            {result.totalPages > 1 && (
              <Box display="flex" justifyContent="center" mt={4}>
                <Pagination
                  page={result.page}
                  totalPages={result.totalPages}
                  onChange={handlePageChange}
                />
              </Box>
            )}
          </>
        ) : null}
      </Box>
    </Stack>
  );
}
