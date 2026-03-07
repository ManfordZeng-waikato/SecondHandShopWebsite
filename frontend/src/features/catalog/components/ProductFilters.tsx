import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Box,
  Chip,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import type { Category } from '../../../entities/category/types';
import type { ProductQueryParams } from '../../../entities/product/types';

interface ProductFiltersProps {
  params: ProductQueryParams;
  categories: Category[];
  onFilterChange: (updates: Partial<ProductQueryParams>) => void;
}

export function ProductFilters({
  params,
  categories,
  onFilterChange,
}: ProductFiltersProps) {
  const [localMinPrice, setLocalMinPrice] = useState(
    params.minPrice?.toString() ?? '',
  );
  const [localMaxPrice, setLocalMaxPrice] = useState(
    params.maxPrice?.toString() ?? '',
  );
  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  useEffect(() => {
    setLocalMinPrice(params.minPrice?.toString() ?? '');
    setLocalMaxPrice(params.maxPrice?.toString() ?? '');
  }, [params.minPrice, params.maxPrice]);

  useEffect(() => {
    return () => clearTimeout(debounceRef.current);
  }, []);

  const debouncedPriceChange = useCallback(
    (key: 'minPrice' | 'maxPrice', value: string) => {
      clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        const num = value === '' ? undefined : parseFloat(value);
        onFilterChange({
          [key]:
            num !== undefined && Number.isFinite(num) && num >= 0
              ? num
              : undefined,
        });
      }, 500);
    },
    [onFilterChange],
  );

  return (
    <Stack spacing={2}>
      {/* Category chips */}
      <Box>
        <Typography
          variant="subtitle2"
          fontWeight={600}
          mb={1}
          color="text.secondary"
        >
          Category
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          <Chip
            label="All"
            variant={!params.category ? 'filled' : 'outlined'}
            onClick={() => onFilterChange({ category: undefined })}
            sx={{
              fontWeight: !params.category ? 700 : 500,
              bgcolor: !params.category ? 'primary.main' : 'transparent',
              color: !params.category
                ? 'primary.contrastText'
                : 'text.primary',
              borderColor: 'divider',
              '&:hover': {
                bgcolor: !params.category ? 'primary.dark' : 'action.hover',
              },
              transition: 'all 0.2s ease',
            }}
          />
          {categories.map((cat) => {
            const isActive = params.category === cat.slug;
            return (
              <Chip
                key={cat.id}
                label={cat.name}
                variant={isActive ? 'filled' : 'outlined'}
                onClick={() =>
                  onFilterChange({
                    category: isActive ? undefined : cat.slug,
                  })
                }
                sx={{
                  fontWeight: isActive ? 700 : 500,
                  bgcolor: isActive ? 'primary.main' : 'transparent',
                  color: isActive ? 'primary.contrastText' : 'text.primary',
                  borderColor: 'divider',
                  '&:hover': {
                    bgcolor: isActive ? 'primary.dark' : 'action.hover',
                  },
                  transition: 'all 0.2s ease',
                }}
              />
            );
          })}
        </Box>
      </Box>

      {/* Price range + Status */}
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        alignItems={{ sm: 'center' }}
        flexWrap="wrap"
        useFlexGap
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <TextField
            size="small"
            label="Min price"
            type="number"
            value={localMinPrice}
            onChange={(e) => {
              setLocalMinPrice(e.target.value);
              debouncedPriceChange('minPrice', e.target.value);
            }}
            slotProps={{ htmlInput: { min: 0, step: 1 } }}
            sx={{ width: 120 }}
          />
          <Typography color="text.secondary">–</Typography>
          <TextField
            size="small"
            label="Max price"
            type="number"
            value={localMaxPrice}
            onChange={(e) => {
              setLocalMaxPrice(e.target.value);
              debouncedPriceChange('maxPrice', e.target.value);
            }}
            slotProps={{ htmlInput: { min: 0, step: 1 } }}
            sx={{ width: 120 }}
          />
        </Stack>

        <ToggleButtonGroup
          size="small"
          value={params.status ?? 'all'}
          exclusive
          onChange={(_, value) => {
            if (value !== null) {
              onFilterChange({
                status: value === 'all' ? undefined : value,
              });
            }
          }}
          sx={{
            '& .MuiToggleButton-root': {
              px: 1.5,
              py: 0.5,
              fontSize: '0.8rem',
              textTransform: 'none',
            },
          }}
        >
          <ToggleButton value="all">All</ToggleButton>
          <ToggleButton value="Available">Available</ToggleButton>
          <ToggleButton value="Sold">Sold</ToggleButton>
        </ToggleButtonGroup>
      </Stack>
    </Stack>
  );
}
