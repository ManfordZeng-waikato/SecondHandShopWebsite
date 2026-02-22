import { Chip } from '@mui/material';
import type { ProductStatus } from '../../entities/product/types';

const statusColorMap: Record<ProductStatus, 'success' | 'warning' | 'default'> = {
  Available: 'success',
  Sold: 'warning',
  OffShelf: 'default',
};

export function StatusChip({ status }: { status: ProductStatus }) {
  return <Chip label={status} color={statusColorMap[status]} size="small" />;
}
