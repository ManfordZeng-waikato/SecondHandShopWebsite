import { useState } from 'react';
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

const FALLBACK_IMAGE = 'https://picsum.photos/seed/no-image/1000/500';

export function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const [selectedIndex, setSelectedIndex] = useState(0);

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

  const { images } = productQuery.data;
  const sortedImages = [...images].sort((a, b) => {
    if (a.isPrimary !== b.isPrimary) return a.isPrimary ? -1 : 1;
    return a.sortOrder - b.sortOrder;
  });
  const activeImage = sortedImages[selectedIndex];
  const activeUrl = activeImage?.displayUrl || FALLBACK_IMAGE;

  return (
    <Card>
      <CardMedia
        component="img"
        height="400"
        image={activeUrl}
        alt={activeImage?.altText ?? productQuery.data.title}
        sx={{ objectFit: 'contain', bgcolor: 'grey.100', p: 2 }}
      />

      {sortedImages.length > 1 && (
        <Box sx={{ display: 'flex', gap: 1, px: 2, py: 1.5, bgcolor: 'grey.50', overflowX: 'auto' }}>
          {sortedImages.map((img, index) => (
            <Box
              key={img.id}
              component="img"
              src={img.displayUrl}
              alt={img.altText ?? `Image ${index + 1}`}
              onClick={() => setSelectedIndex(index)}
              sx={{
                width: 72,
                height: 72,
                objectFit: 'cover',
                borderRadius: 1,
                cursor: 'pointer',
                border: '2px solid',
                borderColor: index === selectedIndex ? 'primary.main' : 'transparent',
                opacity: index === selectedIndex ? 1 : 0.6,
                transition: 'all 0.2s',
                '&:hover': { opacity: 1 },
                flexShrink: 0,
              }}
            />
          ))}
        </Box>
      )}

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
