import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { fetchProductSaleHistory } from '../api/adminApi';
import {
  type ProductSaleDto,
  saleCancellationReasonLabels,
} from '../../../entities/sale/types';

export interface ProductSaleHistoryDialogProps {
  open: boolean;
  productId: string | null;
  productTitle: string;
  onClose: () => void;
}

function formatDate(iso: string | null): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString();
}

function SaleHistoryItem({ sale }: { sale: ProductSaleDto }) {
  const isCompleted = sale.status === 'Completed';

  return (
    <Box sx={{ py: 1.5 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={2}>
        <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              ${sale.finalSoldPrice.toFixed(2)}
            </Typography>
            {sale.listedPriceAtSale !== sale.finalSoldPrice && (
              <Typography variant="caption" color="text.disabled">
                (listed ${sale.listedPriceAtSale.toFixed(2)})
              </Typography>
            )}
            <Chip
              label={sale.status}
              size="small"
              color={isCompleted ? 'success' : 'default'}
              variant={isCompleted ? 'filled' : 'outlined'}
              sx={{ height: 20, fontSize: '0.7rem' }}
            />
          </Stack>
          <Typography variant="caption" color="text.secondary">
            Sold at {formatDate(sale.soldAtUtc)}
            {sale.paymentMethod && ` · ${sale.paymentMethod}`}
          </Typography>
          {(sale.buyerName || sale.buyerEmail || sale.buyerPhone) && (
            <Typography variant="body2" color="text.secondary">
              Buyer:{' '}
              {[sale.buyerName, sale.buyerEmail, sale.buyerPhone].filter(Boolean).join(' · ')}
            </Typography>
          )}
          {sale.notes && (
            <Typography variant="caption" color="text.disabled">
              Note: {sale.notes}
            </Typography>
          )}
          {!isCompleted && (
            <Box sx={{ mt: 0.5, pl: 1, borderLeft: '2px solid', borderColor: 'warning.light' }}>
              <Typography variant="caption" color="warning.dark" sx={{ fontWeight: 600 }}>
                Cancelled {formatDate(sale.cancelledAtUtc)}
                {sale.cancellationReason &&
                  ` — ${saleCancellationReasonLabels[sale.cancellationReason] ?? sale.cancellationReason}`}
              </Typography>
              {sale.cancellationNote && (
                <Typography variant="caption" display="block" color="text.secondary">
                  {sale.cancellationNote}
                </Typography>
              )}
            </Box>
          )}
        </Stack>
      </Stack>
    </Box>
  );
}

export function ProductSaleHistoryDialog({
  open,
  productId,
  productTitle,
  onClose,
}: ProductSaleHistoryDialogProps) {
  const historyQuery = useQuery({
    queryKey: ['product-sale-history', productId],
    queryFn: () => fetchProductSaleHistory(productId!),
    enabled: open && Boolean(productId),
  });

  const sales = historyQuery.data ?? [];

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        Sale History
        <Typography variant="body2" color="text.secondary">
          {productTitle}
        </Typography>
      </DialogTitle>

      <DialogContent dividers>
        {historyQuery.isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : historyQuery.isError ? (
          <Alert severity="error">Failed to load sale history.</Alert>
        ) : sales.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
            No sale records for this product yet.
          </Typography>
        ) : (
          <Stack divider={<Divider flexItem />}>
            {sales.map((sale) => (
              <SaleHistoryItem key={sale.id} sale={sale} />
            ))}
          </Stack>
        )}
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
