import { type FormEvent, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Fade,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Stack,
  Switch,
  TablePagination,
  TextField,
  Typography,
} from '@mui/material';
import ImageIcon from '@mui/icons-material/Image';
import InventoryIcon from '@mui/icons-material/Inventory2Outlined';
import StarIcon from '@mui/icons-material/StarRounded';
import FilterListIcon from '@mui/icons-material/FilterList';
import SearchIcon from '@mui/icons-material/Search';
import type { ProductStatus } from '../entities/product/types';
import {
  type AdminProductListItem,
  type AdminProductSortBy,
  type SortDirection,
  fetchAdminProducts,
  updateProductFeatured,
  updateProductStatus,
} from '../features/admin/api/adminApi';
import { fetchCategories } from '../features/catalog/api/catalogApi';
import { StatusChip } from '../shared/components/StatusChip';
import { ProductSaleDialog } from '../features/admin/components/ProductSaleDialog';

const statusOptions: ProductStatus[] = ['Available', 'Sold', 'OffShelf'];
type FeaturedFilter = 'all' | 'featured' | 'not-featured';
const FEATURED_SORT_ORDER_MIN = 0;
const FEATURED_SORT_ORDER_MAX = 999;
const PAGE_SIZE_OPTIONS = [10, 20, 50];

interface FeaturedDraftState {
  isFeatured: boolean;
  featuredSortOrder: string;
}

function toSortOrderValue(value: number | null): string {
  return value === null ? '' : String(value);
}

function parseSortOrder(value: string): number | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return null;
  }

  if (!/^\d+$/.test(trimmed)) {
    throw new Error('Sort order must be a non-negative integer.');
  }

  const parsed = Number(trimmed);
  if (!Number.isSafeInteger(parsed)) {
    throw new Error('Sort order is too large.');
  }

  if (parsed < FEATURED_SORT_ORDER_MIN || parsed > FEATURED_SORT_ORDER_MAX) {
    throw new Error(
      `Sort order must be between ${FEATURED_SORT_ORDER_MIN} and ${FEATURED_SORT_ORDER_MAX}. ` +
      'Smaller values appear earlier.',
    );
  }

  return parsed;
}

function resolveErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error && typeof error === 'object') {
    const maybeResponse = (error as { response?: { data?: { message?: unknown } } }).response;
    const message = maybeResponse?.data?.message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }
  }

  return fallbackMessage;
}

const filterChipSx = (isActive: boolean) => ({
  fontWeight: isActive ? 700 : 500,
  fontSize: '0.8rem',
  bgcolor: isActive ? 'primary.main' : 'transparent',
  color: isActive ? 'primary.contrastText' : 'text.secondary',
  borderColor: isActive ? 'primary.main' : 'divider',
  '&:hover': {
    bgcolor: isActive ? 'primary.dark' : 'action.hover',
  },
  transition: 'all 0.15s ease',
}) as const;

const productCardSx = {
  p: 0,
  overflow: 'hidden',
  transition: 'border-color 0.15s ease',
  '&:hover': {
    borderColor: '#c0c0c0',
  },
} as const;

export function AdminProductsPage() {
  const queryClient = useQueryClient();

  // Filters
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();
  const [featuredFilter, setFeaturedFilter] = useState<FeaturedFilter>('all');
  const [statusFilter, setStatusFilter] = useState<ProductStatus | 'all'>('all');

  // Search
  const [searchInput, setSearchInput] = useState('');
  const [searchKeyword, setSearchKeyword] = useState('');

  // Sorting
  const [sortBy, setSortBy] = useState<AdminProductSortBy>('updatedAt');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  // Pagination
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // Draft & feedback
  const [featuredDrafts, setFeaturedDrafts] = useState<Record<string, FeaturedDraftState>>({});
  const [feedback, setFeedback] = useState<{ severity: 'success' | 'error'; message: string } | null>(null);

  // Sale dialog
  const [saleTarget, setSaleTarget] = useState<AdminProductListItem | null>(null);

  const featuredFilterParam = useMemo<boolean | undefined>(() => {
    if (featuredFilter === 'all') {
      return undefined;
    }

    return featuredFilter === 'featured';
  }, [featuredFilter]);

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: fetchCategories,
    staleTime: 5 * 60 * 1000,
  });

  const productsQuery = useQuery({
    queryKey: [
      'admin-products',
      page,
      pageSize,
      searchKeyword,
      statusFilter,
      selectedCategoryId,
      featuredFilterParam,
      sortBy,
      sortDirection,
    ],
    queryFn: () => fetchAdminProducts({
      page,
      pageSize,
      search: searchKeyword || undefined,
      status: statusFilter === 'all' ? undefined : statusFilter,
      categoryId: selectedCategoryId,
      isFeatured: featuredFilterParam,
      sortBy,
      sortDirection,
    }),
  });

  const statusMutation = useMutation({
    mutationFn: ({ productId, status }: { productId: string; status: ProductStatus }) =>
      updateProductStatus(productId, status),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
    },
    onError: () => {
      setFeedback({ severity: 'error', message: 'Failed to update product status.' });
    },
  });

  const featuredMutation = useMutation({
    mutationFn: ({
      productId,
      input,
    }: {
      productId: string;
      input: { isFeatured: boolean; featuredSortOrder: number | null };
    }) => updateProductFeatured(productId, input),
    onSuccess: async (_, variables) => {
      setFeaturedDrafts((previous) => {
        const next = { ...previous };
        delete next[variables.productId];
        return next;
      });
      setFeedback({ severity: 'success', message: 'Featured settings updated.' });
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
    },
    onError: (error) => {
      setFeedback({
        severity: 'error',
        message: resolveErrorMessage(error, 'Failed to save featured settings. Please review and retry.'),
      });
    },
  });

  const categories = categoriesQuery.data ?? [];
  const products = productsQuery.data?.items ?? [];
  const totalCount = productsQuery.data?.totalCount ?? 0;

  const getDraft = (product: AdminProductListItem): FeaturedDraftState => {
    return (
      featuredDrafts[product.id] ?? {
        isFeatured: product.isFeatured,
        featuredSortOrder: toSortOrderValue(product.featuredSortOrder),
      }
    );
  };

  const setDraft = (productId: string, nextDraft: FeaturedDraftState) => {
    setFeaturedDrafts((previous) => ({
      ...previous,
      [productId]: nextDraft,
    }));
  };

  const hasFeaturedChanges = (product: AdminProductListItem, draft: FeaturedDraftState): boolean => {
    const sortOrderFromDraft = draft.isFeatured ? parseSortOrder(draft.featuredSortOrder) : null;
    return product.isFeatured !== draft.isFeatured || product.featuredSortOrder !== sortOrderFromDraft;
  };

  const handleSaveFeatured = async (product: AdminProductListItem, draft: FeaturedDraftState) => {
    try {
      const parsedSortOrder = draft.isFeatured ? parseSortOrder(draft.featuredSortOrder) : null;
      await featuredMutation.mutateAsync({
        productId: product.id,
        input: {
          isFeatured: draft.isFeatured,
          featuredSortOrder: parsedSortOrder,
        },
      });
    } catch (error) {
      if (error instanceof Error && error.message.includes('Sort order')) {
        setFeedback({ severity: 'error', message: error.message });
      }
    }
  };

  const handleSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPage(1);
    setSearchKeyword(searchInput.trim());
  };

  const resetFilters = () => {
    setSearchInput('');
    setSearchKeyword('');
    setSelectedCategoryId(undefined);
    setFeaturedFilter('all');
    setStatusFilter('all');
    setPage(1);
  };

  const hasActiveFilters = searchKeyword || selectedCategoryId || featuredFilter !== 'all' || statusFilter !== 'all';

  // --- Page header ---
  const headerSection = (
    <Box sx={{ mb: 1 }}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        justifyContent="space-between"
        alignItems={{ xs: 'flex-start', sm: 'center' }}
        spacing={1}
      >
        <Box>
          <Typography variant="h4" sx={{ lineHeight: 1.2 }}>
            Product Management
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {productsQuery.isLoading
              ? 'Loading...'
              : `${totalCount} ${totalCount === 1 ? 'product' : 'products'} total`}
            {hasActiveFilters ? ' (filtered)' : ''}
          </Typography>
        </Box>
      </Stack>
    </Box>
  );

  // --- Search & sort toolbar ---
  const searchSection = (
    <Paper sx={{ p: 2 }}>
      <Stack
        component="form"
        onSubmit={handleSearch}
        direction={{ xs: 'column', md: 'row' }}
        spacing={1.5}
        alignItems={{ xs: 'stretch', md: 'center' }}
      >
        <TextField
          size="small"
          label="Search by title"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          slotProps={{
            input: {
              startAdornment: <SearchIcon sx={{ fontSize: 18, color: 'text.disabled', mr: 0.5 }} />,
            },
          }}
          sx={{ flex: 1 }}
        />
        <Select
          size="small"
          value={sortBy}
          onChange={(event) => {
            setSortBy(event.target.value as AdminProductSortBy);
            setPage(1);
          }}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="updatedAt">Sort: last updated</MenuItem>
          <MenuItem value="createdAt">Sort: date created</MenuItem>
          <MenuItem value="price">Sort: price</MenuItem>
          <MenuItem value="title">Sort: title</MenuItem>
        </Select>
        <Select
          size="small"
          value={sortDirection}
          onChange={(event) => {
            setSortDirection(event.target.value as SortDirection);
            setPage(1);
          }}
          sx={{ minWidth: 100 }}
        >
          <MenuItem value="desc">Desc</MenuItem>
          <MenuItem value="asc">Asc</MenuItem>
        </Select>
        <Button type="submit" variant="contained" size="small">
          Search
        </Button>
        {hasActiveFilters && (
          <Button variant="outlined" size="small" onClick={resetFilters}>
            Reset
          </Button>
        )}
      </Stack>
    </Paper>
  );

  // --- Filter toolbar ---
  const filterSection = (
    <Paper sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        {/* Status filter */}
        <Box>
          <Stack direction="row" spacing={0.75} alignItems="center" sx={{ mb: 1.25 }}>
            <FilterListIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Status
            </Typography>
          </Stack>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
            <Chip
              label="All"
              size="small"
              variant={statusFilter === 'all' ? 'filled' : 'outlined'}
              onClick={() => { setStatusFilter('all'); setPage(1); }}
              sx={filterChipSx(statusFilter === 'all')}
            />
            {statusOptions.map((status) => (
              <Chip
                key={status}
                label={status}
                size="small"
                variant={statusFilter === status ? 'filled' : 'outlined'}
                onClick={() => { setStatusFilter(statusFilter === status ? 'all' : status); setPage(1); }}
                sx={filterChipSx(statusFilter === status)}
              />
            ))}
          </Box>
        </Box>

        <Divider />

        {/* Category filters */}
        <Box>
          <Stack direction="row" spacing={0.75} alignItems="center" sx={{ mb: 1.25 }}>
            <FilterListIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Category
            </Typography>
          </Stack>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
            <Chip
              label="All"
              size="small"
              variant={!selectedCategoryId ? 'filled' : 'outlined'}
              onClick={() => { setSelectedCategoryId(undefined); setPage(1); }}
              sx={filterChipSx(!selectedCategoryId)}
            />
            {categories.map((cat) => {
              const isActive = selectedCategoryId === cat.id;
              return (
                <Chip
                  key={cat.id}
                  label={cat.name}
                  size="small"
                  variant={isActive ? 'filled' : 'outlined'}
                  onClick={() => { setSelectedCategoryId(isActive ? undefined : cat.id); setPage(1); }}
                  sx={filterChipSx(isActive)}
                />
              );
            })}
          </Box>
        </Box>

        <Divider />

        {/* Featured filter */}
        <Box>
          <Stack direction="row" spacing={0.75} alignItems="center" sx={{ mb: 1.25 }}>
            <StarIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Featured
            </Typography>
          </Stack>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
            {([
              { value: 'all' as const, label: 'All' },
              { value: 'featured' as const, label: 'Featured' },
              { value: 'not-featured' as const, label: 'Not featured' },
            ]).map(({ value, label }) => (
              <Chip
                key={value}
                label={label}
                size="small"
                variant={featuredFilter === value ? 'filled' : 'outlined'}
                onClick={() => { setFeaturedFilter(value); setPage(1); }}
                sx={filterChipSx(featuredFilter === value)}
              />
            ))}
          </Box>
        </Box>
      </Stack>
    </Paper>
  );

  // --- Loading state ---
  if (productsQuery.isLoading) {
    return (
      <Stack spacing={2.5}>
        {headerSection}
        {searchSection}
        {filterSection}
        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', py: 8 }}>
          <CircularProgress size={32} sx={{ mb: 1.5, color: 'text.secondary' }} />
          <Typography variant="body2" color="text.secondary">
            Loading products...
          </Typography>
        </Box>
      </Stack>
    );
  }

  // --- Error state ---
  if (productsQuery.isError) {
    return (
      <Stack spacing={2.5}>
        {headerSection}
        {searchSection}
        {filterSection}
        <Alert severity="error">Unable to load products. Please refresh and try again.</Alert>
      </Stack>
    );
  }

  // --- Empty state ---
  if (products.length === 0) {
    return (
      <Stack spacing={2.5}>
        {headerSection}
        {searchSection}
        {filterSection}
        <Paper sx={{ py: 8, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <InventoryIcon sx={{ fontSize: 48, color: 'grey.300', mb: 2 }} />
          <Typography variant="h6" color="text.secondary" sx={{ mb: 0.5 }}>
            No products found
          </Typography>
          <Typography variant="body2" color="text.disabled">
            {hasActiveFilters
              ? 'Try adjusting the search or filters above.'
              : 'Create your first product to get started.'}
          </Typography>
        </Paper>

        <Snackbar
          open={Boolean(feedback)}
          autoHideDuration={3000}
          onClose={() => setFeedback(null)}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        >
          {feedback ? (
            <Alert onClose={() => setFeedback(null)} severity={feedback.severity} sx={{ width: '100%' }}>
              {feedback.message}
            </Alert>
          ) : undefined}
        </Snackbar>
      </Stack>
    );
  }

  // --- Product list ---
  return (
    <Stack spacing={2.5}>
      {headerSection}
      {searchSection}
      {filterSection}

      {/* Pagination top */}
      <Paper sx={{ overflow: 'hidden' }}>
        <TablePagination
          component="div"
          count={totalCount}
          page={Math.max(0, page - 1)}
          onPageChange={(_, nextPage) => setPage(nextPage + 1)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value));
            setPage(1);
          }}
          rowsPerPageOptions={PAGE_SIZE_OPTIONS}
        />
      </Paper>

      <Stack spacing={1.5}>
        {products.map((product, index) => {
          const draft = getDraft(product);
          const canEnableFeatured = product.status === 'Available';
          const canToggleFeatured = canEnableFeatured || draft.isFeatured;
          const hasChanges = hasFeaturedChanges(product, draft);
          const canSaveFeatured = hasChanges && !(draft.isFeatured && !canEnableFeatured);

          return (
            <Fade in timeout={Math.min(300, 150 + index * 20)} key={product.id}>
              <Paper sx={productCardSx}>
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  sx={{ minHeight: { sm: 120 } }}
                >
                  {/* Image section */}
                  <Box
                    sx={{
                      width: { xs: '100%', sm: 140 },
                      height: { xs: 180, sm: 'auto' },
                      flexShrink: 0,
                      bgcolor: '#fafafa',
                      borderRight: { sm: '1px solid #e2e2e2' },
                      borderBottom: { xs: '1px solid #e2e2e2', sm: 'none' },
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      position: 'relative',
                      overflow: 'hidden',
                    }}
                  >
                    {product.primaryImageUrl ? (
                      <Box
                        component="img"
                        src={product.primaryImageUrl}
                        alt={product.title}
                        loading="lazy"
                        sx={{
                          width: '100%',
                          height: '100%',
                          objectFit: 'cover',
                        }}
                      />
                    ) : (
                      <Avatar
                        variant="rounded"
                        sx={{ width: 56, height: 56, bgcolor: 'transparent' }}
                      >
                        <ImageIcon sx={{ fontSize: 28, color: 'grey.300' }} />
                      </Avatar>
                    )}
                    {/* Featured badge overlay */}
                    {draft.isFeatured && (
                      <Box
                        sx={{
                          position: 'absolute',
                          top: 8,
                          left: 8,
                          bgcolor: '#272727',
                          color: '#fff',
                          borderRadius: '4px',
                          px: 0.75,
                          py: 0.25,
                          display: 'flex',
                          alignItems: 'center',
                          gap: 0.4,
                        }}
                      >
                        <StarIcon sx={{ fontSize: 12 }} />
                        <Typography sx={{ fontSize: '0.65rem', fontWeight: 700, letterSpacing: '0.03em' }}>
                          FEATURED
                        </Typography>
                      </Box>
                    )}
                  </Box>

                  {/* Content section */}
                  <Box sx={{ flex: 1, minWidth: 0, p: 2.5 }}>
                    <Stack spacing={2}>
                      {/* Top row: title + meta */}
                      <Stack
                        direction={{ xs: 'column', md: 'row' }}
                        justifyContent="space-between"
                        alignItems={{ xs: 'flex-start', md: 'center' }}
                        spacing={1}
                      >
                        <Box sx={{ minWidth: 0 }}>
                          <Typography variant="h6" noWrap sx={{ fontSize: '1.05rem' }}>
                            {product.title}
                          </Typography>
                          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mt: 0.25 }}>
                            <Typography variant="body2" sx={{ fontWeight: 600, color: 'text.primary' }}>
                              ${product.price}
                            </Typography>
                            {product.categoryName && (
                              <Typography variant="caption" color="text.secondary">
                                {product.categoryName}
                              </Typography>
                            )}
                            <Typography variant="caption" color="text.disabled">
                              {product.imageCount} {product.imageCount === 1 ? 'image' : 'images'}
                            </Typography>
                          </Stack>
                        </Box>
                        <StatusChip status={product.status} />
                      </Stack>

                      <Divider sx={{ borderColor: '#f0f0f0' }} />

                      {/* Controls row */}
                      <Stack
                        direction={{ xs: 'column', lg: 'row' }}
                        spacing={2}
                        alignItems={{ xs: 'stretch', lg: 'center' }}
                      >
                        {/* Status control */}
                        <FormControl sx={{ minWidth: 160, maxWidth: 200 }} size="small">
                          <InputLabel id={`status-${product.id}`}>Status</InputLabel>
                          <Select
                            labelId={`status-${product.id}`}
                            label="Status"
                            value={product.status}
                            disabled={statusMutation.isPending && statusMutation.variables?.productId === product.id}
                            onChange={(event) =>
                              statusMutation.mutate({
                                productId: product.id,
                                status: event.target.value as ProductStatus,
                              })
                            }
                          >
                            {statusOptions.map((status) => (
                              <MenuItem key={status} value={status}>
                                {status}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>

                        {/* Sale action button */}
                        <Button
                          size="small"
                          variant={product.status === 'Sold' ? 'outlined' : 'contained'}
                          color={product.status === 'Sold' ? 'info' : 'error'}
                          onClick={() => setSaleTarget(product)}
                          sx={{ minWidth: 130, alignSelf: { xs: 'stretch', sm: 'center' } }}
                        >
                          {product.status === 'Sold' ? 'Edit Sale Info' : 'Mark as Sold'}
                        </Button>

                        {/* Vertical divider on desktop */}
                        <Divider
                          orientation="vertical"
                          flexItem
                          sx={{ display: { xs: 'none', lg: 'block' }, borderColor: '#f0f0f0' }}
                        />

                        {/* Featured controls group */}
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1.5}
                          alignItems={{ xs: 'stretch', sm: 'center' }}
                          sx={{
                            flex: 1,
                            bgcolor: draft.isFeatured ? 'rgba(39,39,39,0.025)' : 'transparent',
                            borderRadius: 1,
                            px: draft.isFeatured ? 1.5 : 0,
                            py: draft.isFeatured ? 1 : 0,
                            transition: 'all 0.15s ease',
                          }}
                        >
                          <Stack direction="row" spacing={0.5} alignItems="center">
                            <Switch
                              checked={draft.isFeatured}
                              onChange={(event) => {
                                const previousDraft = draft;
                                setDraft(product.id, {
                                  isFeatured: event.target.checked,
                                  featuredSortOrder: event.target.checked ? previousDraft.featuredSortOrder : '',
                                });
                              }}
                              disabled={featuredMutation.isPending || !canToggleFeatured}
                              size="small"
                              inputProps={{ 'aria-label': `Toggle featured state for ${product.title}` }}
                            />
                            <Typography variant="body2" sx={{ fontWeight: 500, whiteSpace: 'nowrap' }}>
                              Featured
                            </Typography>
                          </Stack>

                          <TextField
                            size="small"
                            label="Sort order"
                            type="number"
                            value={draft.featuredSortOrder}
                            onChange={(event) => {
                              const nextValue = event.target.value;
                              if (!/^\d*$/.test(nextValue)) {
                                return;
                              }

                              const previousDraft = draft;
                              setDraft(product.id, {
                                ...previousDraft,
                                featuredSortOrder: nextValue,
                              });
                            }}
                            disabled={!draft.isFeatured || featuredMutation.isPending || !canEnableFeatured}
                            inputProps={{
                              min: FEATURED_SORT_ORDER_MIN,
                              max: FEATURED_SORT_ORDER_MAX,
                              inputMode: 'numeric',
                            }}
                            helperText={
                              draft.isFeatured
                                ? `${FEATURED_SORT_ORDER_MIN}\u2013${FEATURED_SORT_ORDER_MAX}, smaller = earlier`
                                : 'Enable featured first'
                            }
                            sx={{ width: { xs: '100%', sm: 130 } }}
                          />

                          <Button
                            size="small"
                            variant="contained"
                            disabled={featuredMutation.isPending || !canSaveFeatured}
                            onClick={() => handleSaveFeatured(product, draft)}
                            sx={{ minWidth: 100, alignSelf: { xs: 'stretch', sm: 'center' } }}
                          >
                            {featuredMutation.isPending && featuredMutation.variables?.productId === product.id
                              ? 'Saving...'
                              : 'Save'}
                          </Button>
                        </Stack>
                      </Stack>

                      {!canEnableFeatured && (
                        <Typography variant="caption" color="text.disabled" sx={{ fontStyle: 'italic' }}>
                          Only available products can be featured.
                        </Typography>
                      )}
                    </Stack>
                  </Box>
                </Stack>
              </Paper>
            </Fade>
          );
        })}
      </Stack>

      {/* Pagination bottom */}
      <Paper sx={{ overflow: 'hidden' }}>
        <TablePagination
          component="div"
          count={totalCount}
          page={Math.max(0, page - 1)}
          onPageChange={(_, nextPage) => setPage(nextPage + 1)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value));
            setPage(1);
          }}
          rowsPerPageOptions={PAGE_SIZE_OPTIONS}
        />
      </Paper>

      <ProductSaleDialog
        open={Boolean(saleTarget)}
        productId={saleTarget?.id ?? null}
        productTitle={saleTarget?.title ?? ''}
        productPrice={saleTarget?.price ?? 0}
        onClose={() => setSaleTarget(null)}
        onSaved={() => {
          setSaleTarget(null);
          setFeedback({ severity: 'success', message: 'Sale record saved successfully.' });
        }}
      />

      <Snackbar
        open={Boolean(feedback)}
        autoHideDuration={3000}
        onClose={() => setFeedback(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {feedback ? (
          <Alert onClose={() => setFeedback(null)} severity={feedback.severity} sx={{ width: '100%' }}>
            {feedback.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </Stack>
  );
}
