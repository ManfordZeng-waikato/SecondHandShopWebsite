import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateProductPrice } from '../api/adminApi';

interface EditPriceDialogProps {
  open: boolean;
  productId: string | null;
  productTitle: string;
  currentPrice: number;
  onClose: () => void;
  onSaved: () => void;
}

function resolveErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const maybeResponse = (error as { response?: { data?: { message?: unknown } } }).response;
    const message = maybeResponse?.data?.message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }
  }
  return fallback;
}

export function EditPriceDialog({
  open,
  productId,
  productTitle,
  currentPrice,
  onClose,
  onSaved,
}: EditPriceDialogProps) {
  const queryClient = useQueryClient();
  const [priceInput, setPriceInput] = useState<string>(String(currentPrice));
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: ({ id, price }: { id: string; price: number }) => updateProductPrice(id, price),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      onSaved();
    },
    onError: (error) => {
      setErrorMessage(resolveErrorMessage(error, 'Failed to update price.'));
    },
  });

  const handleSave = () => {
    if (!productId) return;
    setErrorMessage(null);

    const trimmed = priceInput.trim();
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setErrorMessage('Price must be a positive number.');
      return;
    }

    mutation.mutate({ id: productId, price: parsed });
  };

  return (
    <Dialog open={open} onClose={mutation.isPending ? undefined : onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Modify price</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {productTitle}
          </Typography>
          <Typography variant="body2">
            Current price: <strong>${currentPrice}</strong>
          </Typography>
          <TextField
            autoFocus
            label="New price"
            type="number"
            value={priceInput}
            onChange={(event) => setPriceInput(event.target.value)}
            inputProps={{ min: 0.01, step: 0.01, inputMode: 'decimal' }}
            disabled={mutation.isPending}
            fullWidth
          />
          {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button variant="contained" onClick={handleSave} disabled={mutation.isPending}>
          {mutation.isPending ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
