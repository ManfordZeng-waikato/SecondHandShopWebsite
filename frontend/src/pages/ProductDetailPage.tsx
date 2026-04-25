import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
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
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (productQuery.isError) {
    return <Alert severity="error">Unable to load product details.</Alert>;
  }

  if (!productQuery.data) {
    return <Alert severity="warning">Product not found.</Alert>;
  }

  const product = productQuery.data;
  const sortedImages = [...product.images].sort((a, b) => {
    if (a.isPrimary !== b.isPrimary) return a.isPrimary ? -1 : 1;
    return a.sortOrder - b.sortOrder;
  });
  const activeImage = sortedImages[selectedIndex];
  const activeUrl = activeImage?.displayUrl || FALLBACK_IMAGE;

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={{ xs: 3, md: 5 }}
      alignItems={{ md: 'flex-start' }}
    >
      {/* ── Image panel ─────────────────────────────────────────────── */}
      <Box sx={{ flex: { md: '1 1 55%' } }}>
        {/* Main image */}
        <Box
          sx={{
            borderRadius: 3,
            overflow: 'hidden',
            bgcolor: 'grey.100',
            border: '1px solid',
            borderColor: 'divider',
            aspectRatio: '4/3',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Box
            component="img"
            src={activeUrl}
            alt={activeImage?.altText ?? product.title}
            sx={{
              width: '100%',
              height: '100%',
              objectFit: 'contain',
              p: 2,
            }}
          />
        </Box>

        {/* Thumbnails */}
        {sortedImages.length > 1 && (
          <Box sx={{ display: 'flex', gap: 1, mt: 1.5, overflowX: 'auto', pb: 0.5 }}>
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
      </Box>

      {/* ── Details panel ───────────────────────────────────────────── */}
      <Box sx={{ flex: { md: '1 1 45%' } }}>
        {/* Status */}
        <Stack direction="row" spacing={1} alignItems="center" mb={1.5}>
          <StatusChip status={product.status} />
        </Stack>

        <Typography
          variant="h3"
          component="h1"
          sx={{
            fontSize: { xs: '1.8rem', md: '2.2rem' },
            lineHeight: 1.15,
            mb: 1.5,
          }}
        >
          {product.title}
        </Typography>

        <Typography variant="h5" fontWeight={800} color="primary.main" sx={{ mb: 2.5 }}>
          ${product.price.toFixed(2)}
        </Typography>

        <Divider sx={{ mb: 2.5 }} />

        {product.description && (
          <Typography
            color="text.secondary"
            sx={{ lineHeight: 1.8, mb: 3, fontSize: '0.95rem' }}
          >
            {product.description}
          </Typography>
        )}

        {product.status === 'Sold' ? (
          <Button
            variant="contained"
            size="large"
            endIcon={<ArrowForwardIcon />}
            disabled
            sx={{ px: 4, py: 1.5, fontSize: '1rem', borderRadius: 2 }}
          >
            Item Sold
          </Button>
        ) : (
          <Button
            variant="contained"
            size="large"
            component={RouterLink}
            to={`/products/${product.id}/inquiry`}
            endIcon={<ArrowForwardIcon />}
            sx={{ px: 4, py: 1.5, fontSize: '1rem', borderRadius: 2 }}
          >
            Send Inquiry
          </Button>
        )}

        <Typography
          color="text.secondary"
          sx={{ mt: 3, mb: 1, fontSize: '1.05rem', fontWeight: 600, lineHeight: 1.8 }}
        >
          We welcome you to visit in-store to view this item in person.
        </Typography>
      </Box>
    </Stack>
  );
}
