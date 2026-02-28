import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  CardMedia,
  Chip,
  Stack,
  Typography,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import type { Product, ProductCondition } from '../../../entities/product/types';
import { StatusChip } from '../../../shared/components/StatusChip';

const conditionLabels: Record<ProductCondition, string> = {
  LikeNew: 'Like New',
  Good: 'Good',
  Fair: 'Fair',
  NeedsRepair: 'Needs Repair',
};

const conditionColors: Record<ProductCondition, string> = {
  LikeNew: '#2e7d32',
  Good: '#1565c0',
  Fair: '#e65100',
  NeedsRepair: '#c62828',
};

export function ProductCard({ product }: { product: Product }) {
  const primaryImage = product.images.find((item) => item.isPrimary) ?? product.images[0];

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        borderRadius: 3,
        overflow: 'hidden',
        transition: 'transform 0.25s ease, box-shadow 0.25s ease',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: '0 8px 24px rgba(0,0,0,0.12)',
        },
      }}
    >
      <CardActionArea
        component={RouterLink}
        to={`/products/${product.slug}`}
        sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', alignItems: 'stretch' }}
      >
        <Box sx={{ position: 'relative' }}>
          <CardMedia
            component="img"
            height="220"
            image={primaryImage?.url ?? 'https://picsum.photos/seed/fallback/800/500'}
            alt={primaryImage?.altText ?? product.title}
            sx={{ objectFit: 'contain', bgcolor: 'grey.100', p: 1.5 }}
          />
          {product.condition !== 'Good' && (
            <Box sx={{ position: 'absolute', bottom: 10, right: 10 }}>
              <Chip
                label={conditionLabels[product.condition]}
                size="small"
                sx={{
                  fontSize: '0.7rem',
                  fontWeight: 600,
                  height: 22,
                  color: '#fff',
                  bgcolor: conditionColors[product.condition],
                }}
              />
            </Box>
          )}
        </Box>

        <CardContent sx={{ flexGrow: 1, p: 2, '&:last-child': { pb: 2 } }}>
          <Typography
            variant="subtitle1"
            fontWeight={700}
            noWrap
            gutterBottom
            sx={{ lineHeight: 1.3 }}
          >
            {product.title}
          </Typography>

          <Typography
            variant="body2"
            color="text.secondary"
            sx={{
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              overflow: 'hidden',
              lineHeight: 1.5,
              minHeight: '3em',
              mb: 1,
            }}
          >
            {product.description}
          </Typography>

          <Box sx={{ mb: 1.5 }}>
            <StatusChip status={product.status} />
          </Box>

          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" fontWeight={800} color="primary.main">
              ${product.price.toFixed(2)}
            </Typography>
            {product.categoryName && (
              <Typography variant="caption" color="text.secondary">
                {product.categoryName}
              </Typography>
            )}
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
