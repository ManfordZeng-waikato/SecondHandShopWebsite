import { Chip } from '@mui/material';
import type { ProductStatus } from '../../entities/product/types';

const statusLabelMap: Record<ProductStatus, string> = {
  Available: 'Available',
  Sold: 'Sold',
  OffShelf: 'Off shelf',
};

export function StatusChip({ status }: { status: ProductStatus }) {
  return (
    <Chip
      label={statusLabelMap[status]}
      size="small"
      variant="outlined"
      sx={{
        borderColor: 'divider',
        color: 'text.secondary',
        bgcolor: 'transparent',
      }}
    />
  );
}
