/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { AxiosError } from 'axios';
import { revertProductSale } from '../api/adminApi';
import {
  type SaleCancellationReason,
  saleCancellationReasonLabels,
  saleCancellationReasonOptions,
} from '../../../entities/sale/types';

export interface RevertSaleDialogProps {
  open: boolean;
  productId: string | null;
  productTitle: string;
  onClose: () => void;
  onReverted: () => void;
}

function resolveErrorMessage(error: unknown, fallback: string): string {
  const axiosError = error as AxiosError<{ message?: string }>;
  const msg = axiosError?.response?.data?.message;
  return typeof msg === 'string' && msg.trim().length > 0 ? msg : fallback;
}

/**
 * Confirmation dialog for Sold → Available. Requires a cancellation reason and an explicit
 * acknowledgement — the current sale is preserved as a cancelled history row, but this
 * is still a one-way transition from the admin's perspective.
 */
export function RevertSaleDialog({
  open,
  productId,
  productTitle,
  onClose,
  onReverted,
}: RevertSaleDialogProps) {
  const queryClient = useQueryClient();

  const [reason, setReason] = useState<SaleCancellationReason>('BuyerBackedOut');
  const [note, setNote] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);

  useEffect(() => {
    if (open) {
      setReason('BuyerBackedOut');
      setNote('');
      setAcknowledged(false);
    }
  }, [open]);

  const revertMutation = useMutation({
    mutationFn: async () => {
      if (!productId) throw new Error('Missing product id.');
      await revertProductSale(productId, {
        reason,
        cancellationNote: note.trim() || null,
      });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      await queryClient.invalidateQueries({ queryKey: ['product-sale', productId] });
      await queryClient.invalidateQueries({ queryKey: ['product-sale-history', productId] });
      onReverted();
    },
  });

  const canSubmit = acknowledged && !revertMutation.isPending;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        Revert Sale
        <Typography variant="body2" color="text.secondary">
          {productTitle}
        </Typography>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          <Alert severity="warning">
            The current sale record will be marked as <strong>Cancelled</strong> and the product
            will return to the <strong>Available</strong> pool. Buyer, price, and sold time on
            the historical record will be preserved — but this cancellation cannot be undone.
          </Alert>

          {revertMutation.isError && (
            <Alert severity="error">
              {resolveErrorMessage(revertMutation.error, 'Failed to revert sale.')}
            </Alert>
          )}

          <FormControl fullWidth required>
            <InputLabel id="revert-reason-label">Cancellation reason</InputLabel>
            <Select
              labelId="revert-reason-label"
              label="Cancellation reason"
              value={reason}
              onChange={(e) => setReason(e.target.value as SaleCancellationReason)}
            >
              {saleCancellationReasonOptions.map((r) => (
                <MenuItem key={r} value={r}>
                  {saleCancellationReasonLabels[r]}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label="Note (optional)"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            multiline
            minRows={2}
            maxRows={4}
            inputProps={{ maxLength: 2000 }}
            placeholder="Additional context — buyer's message, phone call notes, etc."
            fullWidth
          />

          <FormControlLabel
            control={
              <Checkbox
                checked={acknowledged}
                onChange={(e) => setAcknowledged(e.target.checked)}
              />
            }
            label="I understand this will cancel the current sale and cannot be undone."
          />
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={revertMutation.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="warning"
          onClick={() => revertMutation.mutate()}
          disabled={!canSubmit}
        >
          {revertMutation.isPending ? 'Reverting...' : 'Revert Sale'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
