import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Avatar,
  Box,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import ImageIcon from '@mui/icons-material/Image';
import type { ProductStatus } from '../entities/product/types';
import { fetchAdminProducts, updateProductStatus } from '../features/admin/api/adminApi';
import { fetchCategories } from '../features/catalog/api/catalogApi';
import { StatusChip } from '../shared/components/StatusChip';

const statusOptions: ProductStatus[] = ['Available', 'Sold', 'OffShelf'];

export function AdminProductsPage() {
  const queryClient = useQueryClient();
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();

  const categoriesQuery = useQuery({
    queryKey: ['categories'],
    queryFn: fetchCategories,
    staleTime: 5 * 60 * 1000,
  });

  const productsQuery = useQuery({
    queryKey: ['admin-products', selectedCategoryId],
    queryFn: () => fetchAdminProducts(undefined, selectedCategoryId),
  });

  const statusMutation = useMutation({
    mutationFn: ({ productId, status }: { productId: string; status: ProductStatus }) =>
      updateProductStatus(productId, status),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
    },
  });

  const categories = categoriesQuery.data ?? [];

  if (productsQuery.isLoading) {
    return <CircularProgress />;
  }

  if (productsQuery.isError) {
    return <Alert severity="error">Unable to load products for admin dashboard.</Alert>;
  }

  const products = productsQuery.data ?? [];

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
      {products.map((product) => (
        <Paper key={product.id} sx={{ p: 2 }}>
          <Stack direction="row" spacing={2} alignItems="center">
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
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography variant="h6" noWrap>{product.title}</Typography>
                <StatusChip status={product.status} />
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

              <FormControl sx={{ maxWidth: 220 }} size="small">
                <InputLabel id={`status-${product.id}`}>Status</InputLabel>
                <Select
                  labelId={`status-${product.id}`}
                  label="Status"
                  value={product.status}
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
            </Stack>
          </Stack>
        </Paper>
      ))}
    </Stack>
  );
}
