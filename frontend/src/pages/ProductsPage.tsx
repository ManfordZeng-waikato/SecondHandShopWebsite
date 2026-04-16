import { useCallback, useRef } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Container,
  Stack,
  Typography,
} from '@mui/material';
import SearchOffIcon from '@mui/icons-material/SearchOff';
import RefreshIcon from '@mui/icons-material/Refresh';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
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
    queryKey: [
      'products-paged',
      params.page,
      params.pageSize,
      params.category,
      params.search,
      params.minPrice,
      params.maxPrice,
      params.status,
      params.sort,
    ],
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
    <Container sx={{ py: 4 }}>
      <Stack spacing={3}>
          {/* Page header with logo */}
          <Box
            sx={{
              bgcolor: '#f0ebe4',
              borderRadius: 3,
              overflow: 'hidden',
              position: 'relative',
              px: { xs: 3, sm: 5 },
              py: { xs: 3.5, sm: 4.5 },
            }}
          >
            {/* Dot-grid texture — matches homepage hero */}
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

            <Stack
              direction={{ xs: 'column', md: 'row' }}
              alignItems="center"
              spacing={{ xs: 2.5, md: 3 }}
              sx={{ position: 'relative' }}
            >
              {/* Image column — majority width on desktop */}
              <Box
                sx={{
                  width: '100%',
                  flex: { md: '3 1 0' },
                  minWidth: 0,
                  display: 'flex',
                  justifyContent: 'center',
                  alignItems: 'center',
                  px: { xs: 0, md: 1 },
                }}
              >
                <Box
                  component="img"
                  src="/LogoHome.svg"
                  alt="Pat's Shed"
                  sx={{
                    width: { xs: 'min(100%, 340px)', md: '100%' },
                    maxWidth: '100%',
                    height: 'auto',
                    maxHeight: { xs: 220, sm: 260, md: 340 },
                    objectFit: 'contain',
                  }}
                />
              </Box>

              {/* Vertical divider — desktop only */}
              <Box
                aria-hidden
                sx={{
                  display: { xs: 'none', md: 'block' },
                  width: '1.5px',
                  alignSelf: 'stretch',
                  flexShrink: 0,
                  bgcolor: 'rgba(0,0,0,0.1)',
                  my: 0.5,
                }}
              />

              {/* Copy — narrow column */}
              <Box
                sx={{
                  width: '100%',
                  flex: { md: '1 1 0' },
                  minWidth: 0,
                  maxWidth: { md: 320 },
                  textAlign: { xs: 'center', md: 'left' },
                }}
              >
                <Typography
                  component="span"
                  sx={{
                    display: 'inline-block',
                    mb: { xs: 1, md: 0.75 },
                    letterSpacing: '0.12em',
                    textTransform: 'uppercase',
                    fontSize: '0.65rem',
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
                  variant="h3"
                  component="h1"
                  sx={{
                    fontSize: { xs: '1.85rem', md: '1.65rem' },
                    lineHeight: 1.12,
                    mb: { xs: 0.75, md: 0.5 },
                  }}
                >
                  All Products
                </Typography>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{
                    fontSize: { xs: '0.9rem', md: '0.82rem' },
                    lineHeight: 1.65,
                    maxWidth: { xs: 400, md: 'none' },
                    mx: { xs: 'auto', md: 0 },
                  }}
                >
                  Quality pre-loved furniture &amp; antiques, hand-picked from the&nbsp;Waikato
                </Typography>

                {/* Accent rule — matches homepage hero */}
                <Box
                  aria-hidden
                  sx={{
                    width: 36,
                    height: '2px',
                    bgcolor: 'primary.main',
                    mt: { xs: 1.75, md: 1.25 },
                    mx: { xs: 'auto', md: 0 },
                  }}
                />
              </Box>
            </Stack>
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
                {result.isFallback && params.search ? (
                  <Alert
                    icon={<InfoOutlinedIcon />}
                    severity="info"
                    sx={{ borderRadius: 2 }}
                    action={
                      <Button
                        color="inherit"
                        size="small"
                        onClick={() => setFilters({ search: undefined })}
                      >
                        Clear search
                      </Button>
                    }
                  >
                    No results for "<strong>{params.search}</strong>". Here are our latest products instead.
                  </Alert>
                ) : (
                  <Typography variant="body2" color="text.secondary" mb={2}>
                    {result.totalCount}{' '}
                    {result.totalCount === 1 ? 'item' : 'items'} found
                  </Typography>
                )}

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
    </Container>
  );
}
