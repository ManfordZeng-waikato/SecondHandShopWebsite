import { Chip } from '@mui/material';
import CircleIcon from '@mui/icons-material/Circle';
import type { ProductStatus } from '../../entities/product/types';

const statusConfig: Record<ProductStatus, { label: string; color: string; bg: string }> = {
  Available: { label: 'Available', color: '#2e7d32', bg: 'rgba(46,125,50,0.1)' },
  Sold: { label: 'Sold', color: '#c62828', bg: 'rgba(198,40,40,0.1)' },
  OffShelf: { label: 'Off shelf', color: '#757575', bg: 'rgba(117,117,117,0.1)' },
};

export function StatusChip({ status }: { status: ProductStatus }) {
  const config = statusConfig[status];

  return (
    <Chip
      icon={<CircleIcon sx={{ fontSize: 8, color: `${config.color} !important` }} />}
      label={config.label}
      size="small"
      sx={{
        fontWeight: 600,
        fontSize: '0.7rem',
        height: 24,
        color: config.color,
        bgcolor: config.bg,
        border: 'none',
        backdropFilter: 'blur(8px)',
        '& .MuiChip-icon': { ml: 0.8 },
      }}
    />
  );
}
