import { FormControl, InputLabel, MenuItem, Select } from '@mui/material';
import type { ProductSortOption } from '../../../entities/product/types';

interface ProductSortSelectProps {
  value: ProductSortOption | undefined;
  onChange: (sort: ProductSortOption | undefined) => void;
}

export function ProductSortSelect({
  value,
  onChange,
}: ProductSortSelectProps) {
  return (
    <FormControl size="small" sx={{ minWidth: 180 }}>
      <InputLabel>Sort by</InputLabel>
      <Select
        label="Sort by"
        value={value ?? 'newest'}
        onChange={(e) => {
          const v = e.target.value as ProductSortOption;
          onChange(v === 'newest' ? undefined : v);
        }}
      >
        <MenuItem value="newest">Newest first</MenuItem>
        <MenuItem value="price_asc">Price: Low to High</MenuItem>
        <MenuItem value="price_desc">Price: High to Low</MenuItem>
      </Select>
    </FormControl>
  );
}
