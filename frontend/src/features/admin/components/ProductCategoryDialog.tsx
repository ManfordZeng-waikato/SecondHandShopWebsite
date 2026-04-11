import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material';
import type { CategoryTreeNode } from '../../../entities/category/types';
import {
  fetchProductCategorySelection,
  updateProductCategories,
} from '../api/adminApi';
import { CategoryTreeSelector } from './CategoryTreeSelector';

interface ProductCategoryDialogProps {
  open: boolean;
  productId: string | null;
  productTitle: string;
  categoryTree: CategoryTreeNode[];
  onClose: () => void;
}

function resolveErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error && typeof error === 'object') {
    const maybeResponse = (error as { response?: { data?: { message?: unknown } } }).response;
    const message = maybeResponse?.data?.message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }
  }

  return fallbackMessage;
}

export function ProductCategoryDialog({
  open,
  productId,
  productTitle,
  categoryTree,
  onClose,
}: ProductCategoryDialogProps) {
  const queryClient = useQueryClient();
  const [mainCategoryId, setMainCategoryId] = useState('');
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<string[]>([]);
  const [localError, setLocalError] = useState<string | null>(null);

  const selectionQuery = useQuery({
    queryKey: ['admin-product-category-selection', productId],
    queryFn: () => fetchProductCategorySelection(productId!),
    enabled: open && Boolean(productId),
  });

  useEffect(() => {
    if (!selectionQuery.data) {
      return;
    }

    setMainCategoryId(selectionQuery.data.mainCategoryId);
    setSelectedCategoryIds(selectionQuery.data.selectedCategoryIds);
    setLocalError(null);
  }, [selectionQuery.data]);

  const saveMutation = useMutation({
    mutationFn: () =>
      updateProductCategories(productId!, {
        mainCategoryId,
        selectedCategoryIds,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      await queryClient.invalidateQueries({
        queryKey: ['admin-product-category-selection', productId],
      });
      onClose();
    },
    onError: (error) => {
      setLocalError(resolveErrorMessage(error, 'Failed to save product categories.'));
    },
  });

  const selectedCount = useMemo(() => selectedCategoryIds.length, [selectedCategoryIds]);

  const handleSave = async () => {
    if (!productId) {
      return;
    }

    if (!mainCategoryId) {
      setLocalError('Please choose a main category.');
      return;
    }

    if (selectedCategoryIds.length === 0) {
      setLocalError('Please select at least one category.');
      return;
    }

    setLocalError(null);
    await saveMutation.mutateAsync();
  };

  return (
    <Dialog
      open={open}
      onClose={saveMutation.isPending ? undefined : onClose}
      fullWidth
      maxWidth="md"
    >
      <DialogTitle>Edit product categories</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2}>
          <Box>
            <Typography variant="subtitle1" fontWeight={700}>
              {productTitle}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {selectedCount === 0
                ? 'No categories selected yet.'
                : `${selectedCount} categor${selectedCount === 1 ? 'y' : 'ies'} selected.`}
            </Typography>
          </Box>

          {selectionQuery.isLoading ? (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 4 }}>
              <CircularProgress size={22} />
              <Typography variant="body2" color="text.secondary">
                Loading current category selection...
              </Typography>
            </Box>
          ) : selectionQuery.isError ? (
            <Alert severity="error">Unable to load the current category selection.</Alert>
          ) : (
            <CategoryTreeSelector
              categories={categoryTree}
              selectedCategoryIds={selectedCategoryIds}
              mainCategoryId={mainCategoryId}
              onChange={(nextSelectedCategoryIds, nextMainCategoryId) => {
                setSelectedCategoryIds(nextSelectedCategoryIds);
                setMainCategoryId(nextMainCategoryId);
                setLocalError(null);
              }}
            />
          )}

          {localError && <Alert severity="error">{localError}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saveMutation.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={selectionQuery.isLoading || selectionQuery.isError || saveMutation.isPending}
        >
          {saveMutation.isPending ? 'Saving...' : 'Save categories'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
