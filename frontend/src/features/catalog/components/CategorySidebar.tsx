import { List, ListItemButton, ListItemText, Paper, Typography } from '@mui/material';
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
    <Paper sx={{ p: 2 }}>
      <Typography variant="h6" mb={1}>
        Categories
      </Typography>
      <List disablePadding>
        <ListItemButton selected={!selectedCategoryId} onClick={() => onSelect(undefined)}>
          <ListItemText primary="All products" />
        </ListItemButton>
        {categories.map((category) => (
          <ListItemButton
            key={category.id}
            selected={selectedCategoryId === category.id}
            onClick={() => onSelect(category.id)}
          >
            <ListItemText primary={category.name} />
          </ListItemButton>
        ))}
      </List>
    </Paper>
  );
}
