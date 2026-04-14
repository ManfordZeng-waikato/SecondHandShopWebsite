import { useMemo } from 'react';
import { Box, ButtonBase } from '@mui/material';
import type { Category } from '../../../entities/category/types';

interface CategoryTabsProps {
  categories: Category[];
  activeSlug: string | undefined;
  onChange: (slug: string | undefined) => void;
}

interface CategoryNode {
  id: string;
  name: string;
  slug: string;
  children: CategoryNode[];
}

function buildTree(categories: Category[]): CategoryNode[] {
  const byId = new Map<string, CategoryNode>();
  categories.forEach((c) => {
    byId.set(c.id, { id: c.id, name: c.name, slug: c.slug, children: [] });
  });
  const roots: CategoryNode[] = [];
  categories.forEach((c) => {
    const node = byId.get(c.id)!;
    if (c.parentId && byId.has(c.parentId)) {
      byId.get(c.parentId)!.children.push(node);
    } else {
      roots.push(node);
    }
  });
  return roots;
}

function findBranchForSlug(
  roots: CategoryNode[],
  slug: string | undefined,
): { root: CategoryNode | null; child: CategoryNode | null } {
  if (!slug) return { root: null, child: null };
  for (const root of roots) {
    if (root.slug === slug) return { root, child: null };
    for (const child of root.children) {
      if (child.slug === slug) return { root, child };
    }
  }
  return { root: null, child: null };
}

export function CategoryTabs({
  categories,
  activeSlug,
  onChange,
}: CategoryTabsProps) {
  const tree = useMemo(() => buildTree(categories), [categories]);
  const { root: activeRoot, child: activeChild } = useMemo(
    () => findBranchForSlug(tree, activeSlug),
    [tree, activeSlug],
  );

  return (
    <Box>
      <Box
        role="tablist"
        aria-label="Filter by category"
        sx={{
          display: 'flex',
          alignItems: 'stretch',
          overflowX: 'auto',
          px: { xs: 0.5, sm: 1 },
          '&::-webkit-scrollbar': { display: 'none' },
          scrollbarWidth: 'none',
        }}
      >
        <PrimaryTab
          label="All"
          italic
          active={!activeSlug}
          onClick={() => onChange(undefined)}
        />
        {tree.map((root) => {
          const isActive = activeRoot?.id === root.id;
          return (
            <PrimaryTab
              key={root.id}
              label={root.name}
              active={isActive}
              onClick={() => {
                if (isActive && !activeChild) {
                  onChange(undefined);
                } else {
                  onChange(root.slug);
                }
              }}
            />
          );
        })}
      </Box>

      {activeRoot && activeRoot.children.length > 0 && (
        <Box
          role="tablist"
          aria-label={`Filter by ${activeRoot.name} subcategory`}
          sx={{
            display: 'flex',
            alignItems: 'center',
            overflowX: 'auto',
            px: { xs: 1, sm: 1.5 },
            py: 0.75,
            borderTop: '1px dashed',
            borderColor: 'rgba(0,0,0,0.09)',
            bgcolor: 'rgba(240,235,228,0.45)',
            '&::-webkit-scrollbar': { display: 'none' },
            scrollbarWidth: 'none',
            animation: 'subrowReveal 0.32s cubic-bezier(0.22, 1, 0.36, 1)',
            '@keyframes subrowReveal': {
              from: { opacity: 0, transform: 'translateY(-3px)' },
              to: { opacity: 1, transform: 'translateY(0)' },
            },
          }}
        >
          <SubTab
            label={`All ${activeRoot.name}`}
            active={!activeChild}
            onClick={() => onChange(activeRoot.slug)}
          />
          <Box
            aria-hidden
            sx={{
              width: '1px',
              height: 14,
              bgcolor: 'rgba(0,0,0,0.15)',
              mx: 1,
              flexShrink: 0,
            }}
          />
          {activeRoot.children.map((child) => {
            const isActive = activeChild?.id === child.id;
            return (
              <SubTab
                key={child.id}
                label={child.name}
                active={isActive}
                onClick={() =>
                  onChange(isActive ? activeRoot.slug : child.slug)
                }
              />
            );
          })}
        </Box>
      )}
    </Box>
  );
}

interface PrimaryTabProps {
  label: string;
  active: boolean;
  italic?: boolean;
  onClick: () => void;
}

function PrimaryTab({ label, active, italic, onClick }: PrimaryTabProps) {
  return (
    <ButtonBase
      role="tab"
      aria-selected={active}
      onClick={onClick}
      sx={{
        position: 'relative',
        px: { xs: 1.75, sm: 2.25 },
        py: 1.5,
        fontFamily: "'Fraunces', Georgia, serif",
        fontStyle: italic ? 'italic' : 'normal',
        fontSize: { xs: '1.05rem', sm: '1.18rem' },
        letterSpacing: '0.005em',
        fontWeight: active ? 700 : 500,
        color: active ? 'text.primary' : 'text.secondary',
        whiteSpace: 'nowrap',
        flexShrink: 0,
        transition: 'color 0.18s ease',
        '&:hover': {
          color: 'text.primary',
        },
        '&:hover::after': {
          left: 10,
          right: 10,
        },
        '&::after': {
          content: '""',
          position: 'absolute',
          bottom: 0,
          left: active ? 14 : '50%',
          right: active ? 14 : '50%',
          height: active ? '2px' : '1px',
          bgcolor: active ? 'primary.main' : 'rgba(0,0,0,0.22)',
          transition:
            'left 0.28s cubic-bezier(0.22, 1, 0.36, 1), right 0.28s cubic-bezier(0.22, 1, 0.36, 1), height 0.18s ease',
        },
      }}
    >
      {label}
    </ButtonBase>
  );
}

interface SubTabProps {
  label: string;
  active: boolean;
  onClick: () => void;
}

function SubTab({ label, active, onClick }: SubTabProps) {
  return (
    <ButtonBase
      role="tab"
      aria-selected={active}
      onClick={onClick}
      sx={{
        position: 'relative',
        px: { xs: 1.25, sm: 1.5 },
        py: 0.75,
        fontFamily: "'Fraunces', Georgia, serif",
        fontSize: '0.68rem',
        letterSpacing: '0.11em',
        textTransform: 'uppercase',
        fontWeight: active ? 700 : 500,
        color: active ? 'primary.main' : 'text.secondary',
        whiteSpace: 'nowrap',
        flexShrink: 0,
        transition: 'color 0.16s ease',
        '&:hover': {
          color: 'primary.main',
        },
        '&::before': {
          content: '""',
          position: 'absolute',
          left: active ? 10 : '50%',
          right: active ? 10 : '50%',
          bottom: 2,
          height: '1.5px',
          bgcolor: 'primary.main',
          transition:
            'left 0.22s cubic-bezier(0.22, 1, 0.36, 1), right 0.22s cubic-bezier(0.22, 1, 0.36, 1)',
        },
      }}
    >
      {label}
    </ButtonBase>
  );
}
