import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardMedia,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import { Link as RouterLink, useParams } from 'react-router-dom';
import { fetchProductBySlug } from '../features/catalog/api/catalogApi';
import { StatusChip } from '../shared/components/StatusChip';

export function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();

  const productQuery = useQuery({
    queryKey: ['product', slug],
    queryFn: () => (slug ? fetchProductBySlug(slug) : Promise.resolve(null)),
    enabled: Boolean(slug),
  });

  if (productQuery.isLoading) {
    return <CircularProgress />;
  }

  if (productQuery.isError) {
    return <Alert severity="error">Unable to load product details.</Alert>;
  }

  if (!productQuery.data) {
    return <Alert severity="warning">Product not found.</Alert>;
  }

  const primaryImage = productQuery.data.images.find((item) => item.isPrimary) ?? productQuery.data.images[0];

  return (
    <Card>
      <CardMedia
        component="img"
        height="400"
        image={primaryImage?.url ?? 'https://picsum.photos/seed/no-image/1000/500'}
        alt={primaryImage?.altText ?? productQuery.data.title}
        sx={{ objectFit: 'contain', bgcolor: 'grey.100', p: 2 }}
      />
      <CardContent>
        <Stack spacing={2}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h4">{productQuery.data.title}</Typography>
            <StatusChip status={productQuery.data.status} />
          </Stack>
          <Typography variant="h6">${productQuery.data.price.toFixed(2)}</Typography>
          <Typography color="text.secondary">{productQuery.data.description}</Typography>
          <Typography variant="body2">Condition: {productQuery.data.condition}</Typography>
          <Box>
            <Button
              variant="contained"
              component={RouterLink}
              to={`/products/${productQuery.data.id}/inquiry`}
            >
              Send inquiry
            </Button>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}
