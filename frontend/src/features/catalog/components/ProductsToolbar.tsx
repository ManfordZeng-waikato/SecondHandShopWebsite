import { Box, Chip, Paper, Stack } from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import CloseIcon from '@mui/icons-material/Close';
import type { Category } from '../../../entities/category/types';
import type {
  ProductQueryParams,
  ProductSortOption,
} from '../../../entities/product/types';
import { CategoryTabs } from './CategoryTabs';
import { ProductSortSelect } from './ProductSortSelect';

interface ProductsToolbarProps {
  params: ProductQueryParams;
  categories: Category[];
  onFilterChange: (
    updates: Partial<ProductQueryParams>,
    resetPage?: boolean,
  ) => void;
}

export function ProductsToolbar({
  params,
  categories,
  onFilterChange,
}: ProductsToolbarProps) {
  return (
    <Paper
      sx={{
        borderRadius: 3,
        bgcolor: '#fff',
        overflow: 'hidden',
      }}
    >
      {/* ── Category tabs ────────────────────────────────────────────── */}
      <Box sx={{ borderBottom: '1px solid', borderColor: 'divider', bgcolor: '#fafaf8' }}>
        <CategoryTabs
          categories={categories}
          activeSlug={params.category}
          onChange={(slug) => onFilterChange({ category: slug })}
        />
      </Box>

      {/* ── Bottom bar: search tag + sort ─────────────────────────────── */}
      <Stack
        direction="row"
        alignItems="center"
        sx={{ px: { xs: 2, sm: 2.5 }, py: 1.25, minHeight: 44 }}
      >
        {/* Search tag (if active) */}
        {params.search ? (
          <Chip
            icon={<SearchIcon sx={{ fontSize: '0.85rem !important' }} />}
            label={params.search}
            onDelete={() => onFilterChange({ search: undefined })}
            deleteIcon={<CloseIcon sx={{ fontSize: '0.85rem !important' }} />}
            sx={{
              fontWeight: 500,
              fontSize: '0.82rem',
              height: 28,
              bgcolor: 'primary.main',
              color: '#fff',
              mr: 'auto',
              '& .MuiChip-deleteIcon': {
                color: 'rgba(255,255,255,0.6)',
                '&:hover': { color: '#fff' },
              },
              '& .MuiChip-icon': { color: 'rgba(255,255,255,0.7)' },
            }}
          />
        ) : (
          <Box sx={{ mr: 'auto' }} />
        )}

        <ProductSortSelect
          value={params.sort}
          onChange={(sort: ProductSortOption | undefined) =>
            onFilterChange({ sort })
          }
        />
      </Stack>
    </Paper>
  );
}
