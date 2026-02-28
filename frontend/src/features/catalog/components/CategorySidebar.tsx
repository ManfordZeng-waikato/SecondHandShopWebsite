import { Box, Chip, Paper, Stack, Typography } from '@mui/material';
import CategoryIcon from '@mui/icons-material/Category';
import type { Category } from '../../../entities/category/types';

interface CategorySidebarProps {
  categories: Category[];
  selectedCategoryId?: string;
  onSelect: (categoryId?: string) => void;
}

export function CategorySidebar({
  categories,
  selectedCategoryId,
  onSelect,
}: CategorySidebarProps) {
  return (
    <Paper
      sx={{
        p: 2.5,
        position: 'sticky',
        top: 80,
        borderRadius: 3,
      }}
    >
      <Stack direction="row" alignItems="center" spacing={1} mb={2}>
        <CategoryIcon sx={{ color: 'text.secondary', fontSize: 20 }} />
        <Typography variant="subtitle1" fontWeight={700} letterSpacing={0.3}>
          Categories
        </Typography>
      </Stack>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        <Chip
          label="All"
          variant={!selectedCategoryId ? 'filled' : 'outlined'}
          onClick={() => onSelect(undefined)}
          sx={{
            fontWeight: !selectedCategoryId ? 700 : 500,
            bgcolor: !selectedCategoryId ? 'primary.main' : 'transparent',
            color: !selectedCategoryId ? 'primary.contrastText' : 'text.primary',
            borderColor: 'divider',
            '&:hover': {
              bgcolor: !selectedCategoryId ? 'primary.dark' : 'action.hover',
            },
            transition: 'all 0.2s ease',
          }}
        />
        {categories.map((category) => {
          const isActive = selectedCategoryId === category.id;
          return (
            <Chip
              key={category.id}
              label={category.name}
              variant={isActive ? 'filled' : 'outlined'}
              onClick={() => onSelect(category.id)}
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
    </Paper>
  );
}
