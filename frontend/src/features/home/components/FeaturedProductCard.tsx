import { memo } from 'react';
import { Box, Card, CardActionArea, CardContent, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import type { ProductListItem } from '../../../entities/product/types';
import { StatusChip } from '../../../shared/components/StatusChip';

export const FeaturedProductCard = memo(function FeaturedProductCard({
  product,
}: {
  product: ProductListItem;
}) {
  const sold = product.status === 'Sold';

  return (
    <Card
      sx={{
        height: '100%',
        borderRadius: 3,
        overflow: 'hidden',
        transition: 'transform 0.22s ease, box-shadow 0.22s ease',
        '@media (hover: hover)': {
          '&:hover': {
            transform: 'translateY(-5px)',
            boxShadow: '0 12px 32px rgba(0,0,0,0.08)',
          },
        },
      }}
    >
      <CardActionArea
        component={RouterLink}
        to={`/products/${product.slug}`}
        sx={{ height: '100%', display: 'flex', flexDirection: 'column', alignItems: 'stretch' }}
      >
        {/* Image — 4:3 */}
        <Box
          sx={{
            position: 'relative',
            paddingTop: '75%',
            bgcolor: 'grey.100',
            overflow: 'hidden',
          }}
        >
          <Box
            component="img"
            src={product.coverImageUrl || ''}
            alt={product.title}
            loading="lazy"
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              height: '100%',
              objectFit: 'contain',
              p: 1.5,
              transition: 'transform 0.3s ease',
              ...(sold && { filter: 'grayscale(35%)', opacity: 0.75 }),
              '.MuiCardActionArea-root:hover &': {
                transform: 'scale(1.03)',
              },
            }}
          />
          <Box sx={{ position: 'absolute', top: 8, left: 8 }}>
            <StatusChip status={product.status} />
          </Box>
        </Box>

        <CardContent sx={{ flexGrow: 1, p: 2, '&:last-child': { pb: 2 } }}>
          <Typography
            variant="subtitle2"
            fontWeight={700}
            sx={{
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              overflow: 'hidden',
              lineHeight: 1.35,
              minHeight: '2.7em',
            }}
          >
            {product.title}
          </Typography>

          <Typography
            variant="h6"
            fontWeight={800}
            color="primary.main"
            sx={{ mt: 0.75, fontSize: '1.1rem' }}
          >
            ${product.price.toFixed(2)}
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
});
