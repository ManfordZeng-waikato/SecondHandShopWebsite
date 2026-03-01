import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Avatar,
  Box,
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
import { updateProductStatus } from '../features/admin/api/adminApi';
import { fetchProducts } from '../features/catalog/api/catalogApi';
import { StatusChip } from '../shared/components/StatusChip';

const statusOptions: ProductStatus[] = ['Available', 'Sold', 'OffShelf'];

export function AdminProductsPage() {
  const queryClient = useQueryClient();
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: () => fetchProducts(),
  });

  const statusMutation = useMutation({
    mutationFn: ({ productId, status }: { productId: string; status: ProductStatus }) =>
      updateProductStatus(productId, status),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });

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
      {products.map((product) => {
        const primaryImage = product.images.find((img) => img.isPrimary) ?? product.images[0];

        return (
          <Paper key={product.id} sx={{ p: 2 }}>
            <Stack direction="row" spacing={2} alignItems="center">
              {primaryImage ? (
                <Box
                  component="img"
                  src={primaryImage.displayUrl}
                  alt={primaryImage.altText ?? product.title}
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
                    {product.images.length} image{product.images.length !== 1 ? 's' : ''}
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
        );
      })}
    </Stack>
  );
}
