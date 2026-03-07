import { memo } from 'react';
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
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import { Link as RouterLink } from 'react-router-dom';
import type {
  Product,
  ProductCondition,
  ProductListItem,
} from '../../../entities/product/types';
import { StatusChip } from '../../../shared/components/StatusChip';

type ProductCardItem = Product | ProductListItem;

function isFullProduct(item: ProductCardItem): item is Product {
  return 'images' in item;
}

function getImageUrl(item: ProductCardItem): string {
  if (isFullProduct(item)) {
    const primary = item.images.find((i) => i.isPrimary) ?? item.images[0];
    return primary?.displayUrl || '';
  }
  return item.coverImageUrl || '';
}

function getImageAlt(item: ProductCardItem): string {
  if (isFullProduct(item)) {
    const primary = item.images.find((i) => i.isPrimary) ?? item.images[0];
    return primary?.altText ?? item.title;
  }
  return item.title;
}

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

function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString('en-NZ', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

const isSold = (item: ProductCardItem) => item.status === 'Sold';

export const ProductCard = memo(function ProductCard({
  product,
}: {
  product: ProductCardItem;
}) {
  const imageUrl = getImageUrl(product);
  const imageAlt = getImageAlt(product);
  const sold = isSold(product);
  const description = isFullProduct(product) ? product.description : undefined;

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        borderRadius: 3,
        overflow: 'hidden',
        transition: 'transform 0.2s ease, box-shadow 0.2s ease',
        '&:hover': {
          transform: 'translateY(-3px)',
          boxShadow: '0 6px 20px rgba(0,0,0,0.1)',
        },
      }}
    >
      <CardActionArea
        component={RouterLink}
        to={`/products/${product.slug}`}
        sx={{
          flexGrow: 1,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'stretch',
        }}
      >
        <Box sx={{ position: 'relative' }}>
          <CardMedia
            component="img"
            height="220"
            image={imageUrl}
            alt={imageAlt}
            loading="lazy"
            sx={{
              objectFit: 'contain',
              bgcolor: 'grey.100',
              p: 1.5,
              ...(sold && { filter: 'grayscale(35%)', opacity: 0.8 }),
            }}
          />

          <Box sx={{ position: 'absolute', top: 10, left: 10 }}>
            <StatusChip status={product.status} />
          </Box>

          {product.condition && (
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

        <CardContent
          sx={{ flexGrow: 1, p: 2, '&:last-child': { pb: 2 } }}
        >
          <Typography
            variant="subtitle1"
            fontWeight={700}
            sx={{
              lineHeight: 1.3,
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              overflow: 'hidden',
              minHeight: '2.6em',
            }}
          >
            {product.title}
          </Typography>

          {description && (
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
                mt: 0.5,
              }}
            >
              {description}
            </Typography>
          )}

          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
            mt={1}
          >
            <Typography variant="h6" fontWeight={800} color="primary.main">
              ${product.price.toFixed(2)}
            </Typography>
            {product.categoryName && (
              <Typography variant="caption" color="text.secondary">
                {product.categoryName}
              </Typography>
            )}
          </Stack>

          <Stack
            direction="row"
            alignItems="center"
            spacing={0.5}
            mt={0.5}
          >
            <AccessTimeIcon
              sx={{ fontSize: 14, color: 'text.disabled' }}
            />
            <Typography variant="caption" color="text.disabled">
              {formatDate(product.createdAt)}
            </Typography>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
});
