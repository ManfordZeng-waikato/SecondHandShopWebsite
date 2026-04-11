import { Fragment, useMemo, useState } from 'react';
import {
  Box,
  Checkbox,
  Chip,
  Collapse,
  FormControlLabel,
  IconButton,
  Paper,
  Radio,
  Stack,
  Typography,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined';
import SubdirectoryArrowRightIcon from '@mui/icons-material/SubdirectoryArrowRight';
import type { CategoryTreeNode } from '../../../entities/category/types';

interface CategoryTreeSelectorProps {
  categories: CategoryTreeNode[];
  selectedCategoryIds: string[];
  mainCategoryId: string;
  onChange: (nextSelectedCategoryIds: string[], nextMainCategoryId: string) => void;
}

export function CategoryTreeSelector({
  categories,
  selectedCategoryIds,
  mainCategoryId,
  onChange,
}: CategoryTreeSelectorProps) {
  const [expandedIds, setExpandedIds] = useState<string[]>(() => categories.map((category) => category.id));

  const selectedIdSet = useMemo(() => new Set(selectedCategoryIds), [selectedCategoryIds]);
  const selectedItems = useMemo(
    () => flattenCategoryTree(categories).filter((category) => selectedIdSet.has(category.id)),
    [categories, selectedIdSet],
  );

  const toggleExpanded = (categoryId: string) => {
    setExpandedIds((prev) =>
      prev.includes(categoryId)
        ? prev.filter((id) => id !== categoryId)
        : [...prev, categoryId],
    );
  };

  const handleCheckedChange = (categoryId: string, checked: boolean) => {
    if (checked) {
      const nextSelectedCategoryIds = selectedIdSet.has(categoryId)
        ? selectedCategoryIds
        : [...selectedCategoryIds, categoryId];
      const nextMainCategoryId = mainCategoryId || categoryId;
      onChange(nextSelectedCategoryIds, nextMainCategoryId);
      return;
    }

    const nextSelectedCategoryIds = selectedCategoryIds.filter((id) => id !== categoryId);
    if (nextSelectedCategoryIds.length === 0) {
      onChange([], '');
      return;
    }

    const nextMainCategoryId = mainCategoryId === categoryId
      ? nextSelectedCategoryIds[0]
      : mainCategoryId;
    onChange(nextSelectedCategoryIds, nextMainCategoryId);
  };

  const handleMainCategoryChange = (categoryId: string) => {
    if (!selectedIdSet.has(categoryId)) {
      return;
    }

    onChange(selectedCategoryIds, categoryId);
  };

  return (
    <Stack spacing={2}>
      <Paper
        variant="outlined"
        sx={{
          p: 2,
          borderRadius: 3,
          background:
            'linear-gradient(180deg, rgba(39,39,39,0.03) 0%, rgba(39,39,39,0.01) 100%)',
        }}
      >
        <Stack spacing={0.75}>
          <Stack direction="row" spacing={1} alignItems="center">
            <CategoryOutlinedIcon fontSize="small" sx={{ color: 'text.secondary' }} />
            <Typography variant="subtitle1" fontWeight={700}>
              Categories
            </Typography>
          </Stack>
          <Typography variant="body2" color="text.secondary">
            Tick any categories that apply. Parent and child categories can both be selected.
          </Typography>
        </Stack>

        <Stack spacing={1.25} sx={{ mt: 2 }}>
          {categories.map((category) => (
            <CategoryBranch
              key={category.id}
              category={category}
              depth={0}
              expandedIds={expandedIds}
              selectedIdSet={selectedIdSet}
              mainCategoryId={mainCategoryId}
              onToggleExpanded={toggleExpanded}
              onCheckedChange={handleCheckedChange}
              onMainCategoryChange={handleMainCategoryChange}
            />
          ))}
        </Stack>
      </Paper>

      <Paper
        variant="outlined"
        sx={{
          p: 2,
          borderRadius: 3,
          backgroundColor: 'background.paper',
        }}
      >
        <Stack spacing={1.5}>
          <Typography variant="subtitle1" fontWeight={700}>
            Selection summary
          </Typography>
          {selectedItems.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              No categories selected yet.
            </Typography>
          ) : (
            <>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {selectedItems.map((category) => (
                  <Chip
                    key={category.id}
                    label={category.id === mainCategoryId ? `${category.name} · Main` : category.name}
                    color={category.id === mainCategoryId ? 'primary' : 'default'}
                    variant={category.id === mainCategoryId ? 'filled' : 'outlined'}
                  />
                ))}
              </Box>
              <Typography variant="body2" color="text.secondary">
                Pick one selected category as the main category. This is used for primary navigation and compatibility.
              </Typography>
            </>
          )}
        </Stack>
      </Paper>
    </Stack>
  );
}

interface CategoryBranchProps {
  category: CategoryTreeNode;
  depth: number;
  expandedIds: string[];
  selectedIdSet: Set<string>;
  mainCategoryId: string;
  onToggleExpanded: (categoryId: string) => void;
  onCheckedChange: (categoryId: string, checked: boolean) => void;
  onMainCategoryChange: (categoryId: string) => void;
}

function CategoryBranch({
  category,
  depth,
  expandedIds,
  selectedIdSet,
  mainCategoryId,
  onToggleExpanded,
  onCheckedChange,
  onMainCategoryChange,
}: CategoryBranchProps) {
  const hasChildren = category.children.length > 0;
  const isExpanded = expandedIds.includes(category.id);
  const isSelected = selectedIdSet.has(category.id);
  const isMain = mainCategoryId === category.id;

  return (
    <Fragment>
      <Stack
        direction="row"
        alignItems="center"
        spacing={1}
        sx={{
          pl: depth * 2,
          py: 0.5,
          pr: 1,
          borderRadius: 2,
          backgroundColor: isSelected ? 'rgba(39,39,39,0.04)' : 'transparent',
        }}
      >
        {hasChildren ? (
          <IconButton size="small" onClick={() => onToggleExpanded(category.id)} sx={{ color: 'text.secondary' }}>
            {isExpanded ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
          </IconButton>
        ) : (
          <Box sx={{ width: 32, display: 'flex', justifyContent: 'center', color: 'text.disabled' }}>
            <SubdirectoryArrowRightIcon sx={{ fontSize: 18, opacity: depth === 0 ? 0 : 1 }} />
          </Box>
        )}

        <FormControlLabel
          sx={{ flex: 1, mr: 0 }}
          control={
            <Checkbox
              checked={isSelected}
              onChange={(event) => onCheckedChange(category.id, event.target.checked)}
            />
          }
          label={
            <Stack direction="row" spacing={1} alignItems="center">
              <Typography variant="body1" fontWeight={depth === 0 ? 700 : 500}>
                {category.name}
              </Typography>
              {depth === 0 && (
                <Chip label="Parent" size="small" variant="outlined" sx={{ height: 22 }} />
              )}
            </Stack>
          }
        />

        <FormControlLabel
          sx={{ mr: 0 }}
          control={
            <Radio
              checked={isMain}
              onChange={() => onMainCategoryChange(category.id)}
              disabled={!isSelected}
            />
          }
          label={
            <Typography variant="body2" color={isSelected ? 'text.primary' : 'text.disabled'}>
              Main
            </Typography>
          }
        />
      </Stack>

      {hasChildren && (
        <Collapse in={isExpanded} timeout="auto" unmountOnExit>
          <Stack spacing={0.75} sx={{ mt: 0.5 }}>
            {category.children.map((child) => (
              <CategoryBranch
                key={child.id}
                category={child}
                depth={depth + 1}
                expandedIds={expandedIds}
                selectedIdSet={selectedIdSet}
                mainCategoryId={mainCategoryId}
                onToggleExpanded={onToggleExpanded}
                onCheckedChange={onCheckedChange}
                onMainCategoryChange={onMainCategoryChange}
              />
            ))}
          </Stack>
        </Collapse>
      )}
    </Fragment>
  );
}

function flattenCategoryTree(categories: CategoryTreeNode[]): CategoryTreeNode[] {
  return categories.flatMap((category) => [category, ...flattenCategoryTree(category.children)]);
}
