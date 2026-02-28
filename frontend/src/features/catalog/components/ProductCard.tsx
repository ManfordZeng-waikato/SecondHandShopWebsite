import { Card, CardActions, CardContent, CardMedia, Stack, Typography, Button } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import type { Product } from '../../../entities/product/types';
import { StatusChip } from '../../../shared/components/StatusChip';

export function ProductCard({ product }: { product: Product }) {
  const primaryImage = product.images.find((item) => item.isPrimary) ?? product.images[0];

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardMedia
        component="img"
        height="200"
        image={primaryImage?.url ?? 'https://picsum.photos/seed/fallback/800/500'}
        alt={primaryImage?.altText ?? product.title}
        sx={{ objectFit: 'contain', bgcolor: 'grey.100', p: 1 }}
      />
      <CardContent sx={{ flexGrow: 1 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
          <Typography variant="h6" component="h3">
            {product.title}
          </Typography>
          <StatusChip status={product.status} />
        </Stack>
        <Typography variant="body2" color="text.secondary" mb={2}>
          {product.description}
        </Typography>
        <Typography variant="subtitle1" fontWeight={700}>
          ${product.price.toFixed(2)}
        </Typography>
      </CardContent>
      <CardActions>
        <Button
          component={RouterLink}
          to={`/products/${product.slug}`}
          variant="contained"
          size="small"
          fullWidth
        >
          View details
        </Button>
      </CardActions>
    </Card>
  );
}
