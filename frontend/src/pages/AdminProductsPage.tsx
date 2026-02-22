import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material';
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
      {products.map((product) => (
        <Paper key={product.id} sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <Typography variant="h6">{product.title}</Typography>
              <StatusChip status={product.status} />
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
        </Paper>
      ))}
    </Stack>
  );
}
