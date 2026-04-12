import { useMemo } from 'react';
import {
  Box,
  ButtonBase,
  IconButton,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import CheckRoundedIcon from '@mui/icons-material/CheckRounded';
import StarRoundedIcon from '@mui/icons-material/StarRounded';
import StarOutlineRoundedIcon from '@mui/icons-material/StarOutlineRounded';
import type { CategoryTreeNode } from '../../../entities/category/types';

interface CategoryTreeSelectorProps {
  categories: CategoryTreeNode[];
  selectedCategoryIds: string[];
  mainCategoryId: string;
  onChange: (
    nextSelectedCategoryIds: string[],
    nextMainCategoryId: string,
  ) => void;
}

const SERIF = "'Cormorant Garamond', Georgia, serif";
const SANS = "'DM Sans', sans-serif";

export function CategoryTreeSelector({
  categories,
  selectedCategoryIds,
  mainCategoryId,
  onChange,
}: CategoryTreeSelectorProps) {
  const selectedIdSet = useMemo(
    () => new Set(selectedCategoryIds),
    [selectedCategoryIds],
  );

  const flatIndex = useMemo(() => {
    const index = new Map<string, CategoryTreeNode>();
    const walk = (nodes: CategoryTreeNode[]) => {
      nodes.forEach((node) => {
        index.set(node.id, node);
        walk(node.children);
      });
    };
    walk(categories);
    return index;
  }, [categories]);

  const mainCategoryName = mainCategoryId
    ? flatIndex.get(mainCategoryId)?.name ?? null
    : null;

  const toggleSelection = (categoryId: string) => {
    if (selectedIdSet.has(categoryId)) {
      const next = selectedCategoryIds.filter((id) => id !== categoryId);
      if (next.length === 0) {
        onChange([], '');
        return;
      }
      const nextMain = mainCategoryId === categoryId ? next[0] : mainCategoryId;
      onChange(next, nextMain);
      return;
    }

    const next = [...selectedCategoryIds, categoryId];
    const nextMain = mainCategoryId || categoryId;
    onChange(next, nextMain);
  };

  const setMain = (categoryId: string) => {
    if (!selectedIdSet.has(categoryId)) return;
    onChange(selectedCategoryIds, categoryId);
  };

  const selectedCount = selectedCategoryIds.length;

  return (
    <Stack spacing={2.5}>
      <Paper
        variant="outlined"
        sx={{
          p: { xs: 2, sm: 2.5 },
          borderRadius: 3,
          borderColor: 'rgba(0,0,0,0.12)',
          background:
            'linear-gradient(180deg, #f0ebe4 0%, rgba(240,235,228,0.35) 100%)',
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        <Box
          aria-hidden
          sx={{
            position: 'absolute',
            inset: 0,
            backgroundImage:
              'radial-gradient(circle, rgba(0,0,0,0.05) 1px, transparent 1px)',
            backgroundSize: '18px 18px',
            pointerEvents: 'none',
            opacity: 0.6,
          }}
        />
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={{ xs: 2, sm: 3 }}
          alignItems={{ xs: 'flex-start', sm: 'center' }}
          sx={{ position: 'relative' }}
        >
          <Box sx={{ flex: 1 }}>
            <Overline>Selection</Overline>
            <Typography
              sx={{
                fontFamily: SERIF,
                fontSize: { xs: '1.5rem', sm: '1.75rem' },
                lineHeight: 1.1,
                fontWeight: 700,
                color: 'text.primary',
                mt: 0.25,
              }}
            >
              {selectedCount === 0
                ? 'Nothing selected'
                : `${selectedCount} categor${selectedCount === 1 ? 'y' : 'ies'}`}
            </Typography>
          </Box>

          <Box
            aria-hidden
            sx={{
              display: { xs: 'none', sm: 'block' },
              width: '1px',
              alignSelf: 'stretch',
              bgcolor: 'rgba(0,0,0,0.12)',
            }}
          />

          <Box sx={{ flex: 1.2, minWidth: 0 }}>
            <Overline>Main category</Overline>
            <Typography
              sx={{
                fontFamily: SERIF,
                fontSize: { xs: '1.2rem', sm: '1.35rem' },
                lineHeight: 1.2,
                fontWeight: 600,
                fontStyle: mainCategoryName ? 'normal' : 'italic',
                color: mainCategoryName ? 'text.primary' : 'text.disabled',
                mt: 0.25,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {mainCategoryName ?? 'none yet'}
            </Typography>
          </Box>
        </Stack>
      </Paper>

      <Stack spacing={2}>
        {categories.map((root, index) => (
          <DepartmentPanel
            key={root.id}
            index={index}
            root={root}
            selectedIdSet={selectedIdSet}
            mainCategoryId={mainCategoryId}
            onToggle={toggleSelection}
            onSetMain={setMain}
          />
        ))}
      </Stack>

      <Typography
        variant="caption"
        sx={{
          fontFamily: SANS,
          color: 'text.secondary',
          fontSize: '0.72rem',
          letterSpacing: '0.02em',
          lineHeight: 1.6,
          display: 'block',
        }}
      >
        Tap a category to select it. Click the ★ on any selected category to
        promote it as the main category used for navigation and legacy
        compatibility.
      </Typography>
    </Stack>
  );
}

interface DepartmentPanelProps {
  index: number;
  root: CategoryTreeNode;
  selectedIdSet: Set<string>;
  mainCategoryId: string;
  onToggle: (categoryId: string) => void;
  onSetMain: (categoryId: string) => void;
}

function DepartmentPanel({
  index,
  root,
  selectedIdSet,
  mainCategoryId,
  onToggle,
  onSetMain,
}: DepartmentPanelProps) {
  const selectedChildCount = useMemo(
    () => root.children.filter((c) => selectedIdSet.has(c.id)).length,
    [root.children, selectedIdSet],
  );
  const hasChildren = root.children.length > 0;
  const rootSelected = selectedIdSet.has(root.id);

  return (
    <Paper
      variant="outlined"
      sx={{
        p: { xs: 2, sm: 2.5 },
        borderRadius: 3,
        borderColor: 'rgba(0,0,0,0.12)',
        bgcolor: '#fff',
        transition: 'border-color 0.2s ease, box-shadow 0.2s ease',
        '&:hover': {
          borderColor: 'rgba(0,0,0,0.25)',
        },
      }}
    >
      <Stack
        direction="row"
        alignItems="baseline"
        spacing={1.5}
        sx={{ mb: 1.75 }}
      >
        <Typography
          sx={{
            fontFamily: SANS,
            fontSize: '0.72rem',
            fontWeight: 600,
            letterSpacing: '0.14em',
            color: 'text.disabled',
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          {String(index + 1).padStart(2, '0')}
        </Typography>
        <Typography
          sx={{
            fontFamily: SERIF,
            fontSize: { xs: '1.35rem', sm: '1.55rem' },
            fontWeight: 700,
            lineHeight: 1,
            color: 'text.primary',
            flex: 1,
          }}
        >
          {root.name}
        </Typography>
        {hasChildren && (
          <Overline sx={{ whiteSpace: 'nowrap' }}>
            {selectedChildCount > 0
              ? `${selectedChildCount} / ${root.children.length} selected`
              : `${root.children.length} types`}
          </Overline>
        )}
      </Stack>

      <Box
        aria-hidden
        sx={{
          width: 32,
          height: '1.5px',
          bgcolor: 'primary.main',
          mb: 1.75,
        }}
      />

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        {!hasChildren && (
          <CategoryPill
            label={root.name}
            selected={rootSelected}
            main={mainCategoryId === root.id}
            onToggleSelect={() => onToggle(root.id)}
            onSetMain={() => onSetMain(root.id)}
          />
        )}
        {root.children.map((child) => (
          <CategoryPill
            key={child.id}
            label={child.name}
            selected={selectedIdSet.has(child.id)}
            main={mainCategoryId === child.id}
            onToggleSelect={() => onToggle(child.id)}
            onSetMain={() => onSetMain(child.id)}
          />
        ))}
      </Box>
    </Paper>
  );
}

interface CategoryPillProps {
  label: string;
  selected: boolean;
  main: boolean;
  onToggleSelect: () => void;
  onSetMain: () => void;
}

function CategoryPill({
  label,
  selected,
  main,
  onToggleSelect,
  onSetMain,
}: CategoryPillProps) {
  return (
    <Box sx={{ position: 'relative', display: 'inline-flex' }}>
      <ButtonBase
        onClick={onToggleSelect}
        aria-pressed={selected}
        sx={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 0.65,
          pl: selected ? 1.5 : 1.75,
          pr: selected ? 4.25 : 1.75,
          py: 0.85,
          minHeight: 34,
          borderRadius: '999px',
          border: '1px solid',
          borderColor: selected ? 'primary.main' : 'rgba(0,0,0,0.18)',
          bgcolor: selected ? 'primary.main' : 'transparent',
          color: selected ? '#ffffff' : 'text.primary',
          fontFamily: SANS,
          fontSize: '0.82rem',
          fontWeight: selected ? 600 : 500,
          letterSpacing: '0.005em',
          transition:
            'background-color 0.18s ease, border-color 0.18s ease, color 0.18s ease, padding 0.22s ease',
          '&:hover': {
            borderColor: selected ? 'primary.main' : 'text.primary',
            bgcolor: selected ? '#363636' : 'rgba(0,0,0,0.035)',
          },
        }}
      >
        {selected && (
          <CheckRoundedIcon
            sx={{
              fontSize: 15,
              opacity: 0.95,
            }}
          />
        )}
        {label}
      </ButtonBase>
      {selected && (
        <Tooltip
          title={main ? 'Main category' : 'Set as main category'}
          placement="top"
          arrow
        >
          <IconButton
            size="small"
            onClick={onSetMain}
            aria-label={main ? 'Main category' : 'Set as main category'}
            sx={{
              position: 'absolute',
              right: 3,
              top: '50%',
              transform: 'translateY(-50%)',
              p: 0.35,
              color: main ? '#ffcf4d' : 'rgba(255,255,255,0.55)',
              bgcolor: 'transparent',
              transition: 'color 0.18s ease, background-color 0.18s ease',
              '&:hover': {
                color: '#ffcf4d',
                bgcolor: 'rgba(255,255,255,0.12)',
              },
            }}
          >
            {main ? (
              <StarRoundedIcon sx={{ fontSize: 16 }} />
            ) : (
              <StarOutlineRoundedIcon sx={{ fontSize: 16 }} />
            )}
          </IconButton>
        </Tooltip>
      )}
    </Box>
  );
}

interface OverlineProps {
  children: React.ReactNode;
  sx?: React.ComponentProps<typeof Typography>['sx'];
}

function Overline({ children, sx }: OverlineProps) {
  return (
    <Typography
      component="span"
      sx={{
        display: 'inline-block',
        fontFamily: SANS,
        fontSize: '0.66rem',
        fontWeight: 600,
        letterSpacing: '0.14em',
        textTransform: 'uppercase',
        color: 'text.secondary',
        ...sx,
      }}
    >
      {children}
    </Typography>
  );
}
