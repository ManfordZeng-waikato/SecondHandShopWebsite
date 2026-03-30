import { MenuItem, Select } from '@mui/material';
import SortIcon from '@mui/icons-material/Sort';
import type { ProductSortOption } from '../../../entities/product/types';

interface ProductSortSelectProps {
  value: ProductSortOption | undefined;
  onChange: (sort: ProductSortOption | undefined) => void;
}

const options = [
  { value: 'newest', label: 'Newest first' },
  { value: 'price_asc', label: 'Price: Low → High' },
  { value: 'price_desc', label: 'Price: High → Low' },
] as const;

export function ProductSortSelect({
  value,
  onChange,
}: ProductSortSelectProps) {
  return (
    <Select
      size="small"
      variant="outlined"
      value={value ?? 'newest'}
      onChange={(e) => {
        const v = e.target.value as ProductSortOption;
        onChange(v === 'newest' ? undefined : v);
      }}
      startAdornment={
        <SortIcon sx={{ fontSize: 16, color: 'text.secondary', mr: 0.75 }} />
      }
      sx={{
        fontSize: '0.8rem',
        fontWeight: 500,
        color: 'text.secondary',
        minWidth: 170,
        borderRadius: '8px',
        '& .MuiOutlinedInput-notchedOutline': {
          borderColor: 'divider',
        },
        '&:hover .MuiOutlinedInput-notchedOutline': {
          borderColor: 'text.disabled',
        },
        '& .MuiSelect-select': {
          py: '6px',
          pl: 0,
        },
      }}
    >
      {options.map((opt) => (
        <MenuItem
          key={opt.value}
          value={opt.value}
          sx={{ fontSize: '0.82rem' }}
        >
          {opt.label}
        </MenuItem>
      ))}
    </Select>
  );
}
