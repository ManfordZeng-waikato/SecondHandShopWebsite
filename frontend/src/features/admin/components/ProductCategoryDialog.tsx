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
      PaperProps={{
        sx: {
          borderRadius: 3,
          border: '1px solid rgba(0,0,0,0.12)',
        },
      }}
    >
      <DialogTitle sx={{ pt: 3, pb: 1.5, px: { xs: 3, sm: 4 } }}>
        <Typography
          component="span"
          sx={{
            display: 'block',
            fontFamily: "'Fraunces', Georgia, serif",
            fontSize: '0.68rem',
            fontWeight: 600,
            letterSpacing: '0.14em',
            textTransform: 'uppercase',
            color: 'text.secondary',
            mb: 0.5,
          }}
        >
          Curate this product
        </Typography>
        <Typography
          component="span"
          sx={{
            display: 'block',
            fontFamily: "'Fraunces', Georgia, serif",
            fontSize: { xs: '1.75rem', sm: '2rem' },
            lineHeight: 1.1,
            fontWeight: 700,
            color: 'text.primary',
          }}
        >
          {productTitle}
        </Typography>
        <Box
          aria-hidden
          sx={{
            width: 40,
            height: '2px',
            bgcolor: 'primary.main',
            mt: 1.25,
          }}
        />
      </DialogTitle>
      <DialogContent
        dividers
        sx={{
          px: { xs: 3, sm: 4 },
          py: 3,
          bgcolor: '#fafaf8',
        }}
      >
        <Stack spacing={2}>
          {selectionQuery.isLoading ? (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.5,
                py: 6,
                justifyContent: 'center',
              }}
            >
              <CircularProgress size={20} sx={{ color: 'text.secondary' }} />
              <Typography
                sx={{
                  fontFamily: "'Fraunces', Georgia, serif",
                  fontSize: '0.8rem',
                  letterSpacing: '0.02em',
                  color: 'text.secondary',
                }}
              >
                Loading current category selection…
              </Typography>
            </Box>
          ) : selectionQuery.isError ? (
            <Alert severity="error" sx={{ borderRadius: 2 }}>
              Unable to load the current category selection.
            </Alert>
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

          {localError && (
            <Alert severity="error" sx={{ borderRadius: 2 }}>
              {localError}
            </Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: { xs: 3, sm: 4 }, py: 2, gap: 1 }}>
        <Button
          onClick={onClose}
          disabled={saveMutation.isPending}
          sx={{ color: 'text.secondary' }}
        >
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={
            selectionQuery.isLoading ||
            selectionQuery.isError ||
            saveMutation.isPending
          }
          sx={{ px: 3 }}
        >
          {saveMutation.isPending
            ? 'Saving…'
            : selectedCount === 0
              ? 'Save'
              : `Save ${selectedCount} categor${selectedCount === 1 ? 'y' : 'ies'}`}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
