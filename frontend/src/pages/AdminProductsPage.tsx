import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import ImageIcon from '@mui/icons-material/Image';
import type { ProductStatus } from '../entities/product/types';
import {
  type AdminProductListItem,
  fetchAdminProducts,
  updateProductFeatured,
  updateProductStatus,
} from '../features/admin/api/adminApi';
import { fetchCategories } from '../features/catalog/api/catalogApi';
import { StatusChip } from '../shared/components/StatusChip';

const statusOptions: ProductStatus[] = ['Available', 'Sold', 'OffShelf'];
type FeaturedFilter = 'all' | 'featured' | 'not-featured';
const FEATURED_SORT_ORDER_MIN = 0;
const FEATURED_SORT_ORDER_MAX = 999;

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

export function AdminProductsPage() {
  const queryClient = useQueryClient();
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();
  const [featuredFilter, setFeaturedFilter] = useState<FeaturedFilter>('all');
  const [featuredDrafts, setFeaturedDrafts] = useState<Record<string, FeaturedDraftState>>({});
  const [feedback, setFeedback] = useState<{ severity: 'success' | 'error'; message: string } | null>(null);

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
    queryKey: ['admin-products', selectedCategoryId, featuredFilterParam],
    queryFn: () => fetchAdminProducts({
      categoryId: selectedCategoryId,
      isFeatured: featuredFilterParam,
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

  if (productsQuery.isLoading) {
    return <CircularProgress />;
  }

  if (productsQuery.isError) {
    return <Alert severity="error">Unable to load products for admin dashboard.</Alert>;
  }

  const products = productsQuery.data?.items ?? [];

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

  return (
    <Stack spacing={2}>
      <Typography variant="h4">Manage products</Typography>

      {/* Category filter */}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        <Chip
          label="All categories"
          variant={!selectedCategoryId ? 'filled' : 'outlined'}
          onClick={() => setSelectedCategoryId(undefined)}
          sx={{
            fontWeight: !selectedCategoryId ? 700 : 500,
            bgcolor: !selectedCategoryId ? 'primary.main' : 'transparent',
            color: !selectedCategoryId ? 'primary.contrastText' : 'text.primary',
            borderColor: 'divider',
            '&:hover': {
              bgcolor: !selectedCategoryId ? 'primary.dark' : 'action.hover',
            },
            transition: 'all 0.2s ease',
          }}
        />
        {categories.map((cat) => {
          const isActive = selectedCategoryId === cat.id;
          return (
            <Chip
              key={cat.id}
              label={cat.name}
              variant={isActive ? 'filled' : 'outlined'}
              onClick={() => setSelectedCategoryId(isActive ? undefined : cat.id)}
              sx={{
                fontWeight: isActive ? 700 : 500,
                bgcolor: isActive ? 'primary.main' : 'transparent',
                color: isActive ? 'primary.contrastText' : 'text.primary',
                borderColor: 'divider',
                '&:hover': {
                  bgcolor: isActive ? 'primary.dark' : 'action.hover',
                },
                transition: 'all 0.2s ease',
              }}
            />
          );
        })}
      </Box>

      {/* Featured filter */}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        <Chip
          label="All products"
          variant={featuredFilter === 'all' ? 'filled' : 'outlined'}
          onClick={() => setFeaturedFilter('all')}
        />
        <Chip
          label="Featured only"
          variant={featuredFilter === 'featured' ? 'filled' : 'outlined'}
          onClick={() => setFeaturedFilter('featured')}
        />
        <Chip
          label="Not featured"
          variant={featuredFilter === 'not-featured' ? 'filled' : 'outlined'}
          onClick={() => setFeaturedFilter('not-featured')}
        />
      </Box>

      {/* Product count */}
      <Typography variant="body2" color="text.secondary">
        {products.length} {products.length === 1 ? 'product' : 'products'}
        {selectedCategoryId && categories.length > 0
          ? ` in ${categories.find((c) => c.id === selectedCategoryId)?.name ?? 'selected category'}`
          : ''}
      </Typography>

      {products.length === 0 && (
        <Typography color="text.secondary">
          {selectedCategoryId
            ? 'No products found in this category.'
            : 'No products yet. Create one to get started.'}
        </Typography>
      )}
      {products.map((product) => {
        const draft = getDraft(product);
        const canEnableFeatured = product.status === 'Available';
        const canToggleFeatured = canEnableFeatured || draft.isFeatured;
        const hasChanges = hasFeaturedChanges(product, draft);
        const canSaveFeatured = hasChanges && !(draft.isFeatured && !canEnableFeatured);

        return (
          <Paper key={product.id} sx={{ p: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ xs: 'stretch', sm: 'center' }}>
            {product.primaryImageUrl ? (
              <Box
                component="img"
                src={product.primaryImageUrl}
                alt={product.title}
                sx={{
                  width: 80,
                  height: 80,
                  borderRadius: 1.5,
                  objectFit: 'cover',
                  flexShrink: 0,
                  bgcolor: '#f5f5f5',
                }}
              />
            ) : (
              <Avatar
                variant="rounded"
                sx={{ width: 80, height: 80, bgcolor: 'grey.100', flexShrink: 0 }}
              >
                <ImageIcon sx={{ fontSize: 32, color: 'grey.400' }} />
              </Avatar>
            )}

            <Stack spacing={1} sx={{ flex: 1, minWidth: 0 }}>
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                justifyContent="space-between"
                alignItems={{ xs: 'flex-start', md: 'center' }}
                spacing={1}
              >
                <Typography variant="h6" noWrap>{product.title}</Typography>
                <Stack direction="row" spacing={1} alignItems="center">
                  <StatusChip status={product.status} />
                  {draft.isFeatured && (
                    <Chip size="small" color="secondary" label="Featured" />
                  )}
                </Stack>
              </Stack>

              <Stack direction="row" spacing={2} alignItems="center">
                <Typography variant="body2" color="text.secondary">
                  ${product.price}
                </Typography>
                {product.categoryName && (
                  <Typography variant="body2" color="text.secondary">
                    {product.categoryName}
                  </Typography>
                )}
                <Typography variant="caption" color="text.disabled">
                  {product.imageCount} image{product.imageCount !== 1 ? 's' : ''}
                </Typography>
              </Stack>

              <Stack
                direction={{ xs: 'column', lg: 'row' }}
                spacing={1.5}
                alignItems={{ xs: 'stretch', lg: 'center' }}
              >
                <FormControl sx={{ minWidth: 180, maxWidth: 220 }} size="small">
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

                <Stack direction="row" spacing={1} alignItems="center">
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
                    inputProps={{ 'aria-label': `Toggle featured state for ${product.title}` }}
                  />
                  <Typography variant="body2">Featured</Typography>
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
                      ? `${FEATURED_SORT_ORDER_MIN}-${FEATURED_SORT_ORDER_MAX}, smaller values appear earlier`
                      : 'Enable featured to edit'
                  }
                  sx={{ width: { xs: '100%', sm: 130 } }}
                />

                <Button
                  size="small"
                  variant="contained"
                  disabled={featuredMutation.isPending || !canSaveFeatured}
                  onClick={() => handleSaveFeatured(product, draft)}
                >
                  {featuredMutation.isPending && featuredMutation.variables?.productId === product.id
                    ? 'Saving...'
                    : 'Save featured'}
                </Button>
              </Stack>

              {!canEnableFeatured && (
                <Typography variant="caption" color="text.secondary">
                  Only products with status Available can be featured.
                </Typography>
              )}
            </Stack>
          </Stack>
        </Paper>
        );
      })}

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
