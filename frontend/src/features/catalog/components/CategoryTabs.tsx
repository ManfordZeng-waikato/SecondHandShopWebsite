import { Box, ButtonBase } from '@mui/material';
import type { Category } from '../../../entities/category/types';

interface CategoryTabsProps {
  categories: Category[];
  activeSlug: string | undefined;
  onChange: (slug: string | undefined) => void;
}

export function CategoryTabs({
  categories,
  activeSlug,
  onChange,
}: CategoryTabsProps) {
  return (
    <Box
      role="tablist"
      aria-label="Filter by category"
      sx={{
        display: 'flex',
        overflowX: 'auto',
        px: { xs: 0.5, sm: 1 },
        '&::-webkit-scrollbar': { display: 'none' },
        scrollbarWidth: 'none',
      }}
    >
      <Tab
        label="All"
        active={!activeSlug}
        onClick={() => onChange(undefined)}
      />

      {categories.map((cat) => (
        <Tab
          key={cat.id}
          label={cat.name}
          active={activeSlug === cat.slug}
          onClick={() =>
            onChange(activeSlug === cat.slug ? undefined : cat.slug)
          }
        />
      ))}
    </Box>
  );
}

function Tab({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <ButtonBase
      role="tab"
      aria-selected={active}
      onClick={onClick}
      sx={{
        position: 'relative',
        px: { xs: 1.5, sm: 2 },
        py: 1.25,
        fontSize: '0.85rem',
        fontFamily: 'inherit',
        fontWeight: active ? 600 : 400,
        color: active ? 'text.primary' : 'text.secondary',
        whiteSpace: 'nowrap',
        flexShrink: 0,
        transition: 'color 0.18s ease',
        '&:hover': {
          color: 'text.primary',
          bgcolor: 'rgba(0,0,0,0.02)',
        },
        '&::after': {
          content: '""',
          position: 'absolute',
          bottom: 0,
          left: active ? 12 : '50%',
          right: active ? 12 : '50%',
          height: '2px',
          bgcolor: 'primary.main',
          borderRadius: 1,
          transition: 'left 0.22s ease, right 0.22s ease',
        },
      }}
    >
      {label}
    </ButtonBase>
  );
}
