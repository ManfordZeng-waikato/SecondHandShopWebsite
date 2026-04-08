import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { AxiosError } from 'axios';
import type { ProductSaleDto, SaveProductSaleInput } from '../../../entities/sale/types';
import { paymentMethodOptions, paymentMethodLabels } from '../../../entities/sale/types';
import type { CustomerListItem } from '../../../entities/customer/types';
import {
  createProductSale,
  fetchAdminCustomers,
  fetchProductSale,
  updateProductSale,
} from '../api/adminApi';

export interface ProductSaleDialogProps {
  open: boolean;
  productId: string | null;
  productTitle: string;
  productPrice: number;
  onClose: () => void;
  onSaved: () => void;
}

function toLocalDateTimeValue(utcIso: string | null): string {
  if (!utcIso) {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
  }
  const hasTimeZone = /([zZ]|[+-]\d{2}:\d{2})$/.test(utcIso);
  const d = new Date(hasTimeZone ? utcIso : `${utcIso}Z`);
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
}

function localDateTimeToUtcIso(localValue: string): string {
  return new Date(localValue).toISOString();
}

function resolveErrorMessage(error: unknown, fallback: string): string {
  const axiosError = error as AxiosError<{ message?: string }>;
  const msg = axiosError?.response?.data?.message;
  return typeof msg === 'string' && msg.trim().length > 0 ? msg : fallback;
}

export function ProductSaleDialog({
  open,
  productId,
  productTitle,
  productPrice,
  onClose,
  onSaved,
}: ProductSaleDialogProps) {
  const queryClient = useQueryClient();

  // Form state
  const [buyerName, setBuyerName] = useState('');
  const [buyerPhone, setBuyerPhone] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [finalSoldPrice, setFinalSoldPrice] = useState('');
  const [soldAtUtc, setSoldAtUtc] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [notes, setNotes] = useState('');
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerListItem | null>(null);
  const [customerSearch, setCustomerSearch] = useState('');

  // Fetch existing sale record
  const saleQuery = useQuery({
    queryKey: ['product-sale', productId],
    queryFn: () => fetchProductSale(productId!),
    enabled: open && Boolean(productId),
  });

  // Customer search for autocomplete
  const customerSearchQuery = useQuery({
    queryKey: ['admin-customers-search', customerSearch],
    queryFn: () => fetchAdminCustomers({ search: customerSearch, pageSize: 10 }),
    enabled: open && customerSearch.length >= 2,
    staleTime: 30_000,
  });

  const existingSale: ProductSaleDto | null = saleQuery.data ?? null;
  const isUpdate = existingSale !== null;

  // Reset form when dialog opens or sale data loads
  useEffect(() => {
    if (!open) return;

    if (existingSale) {
      setBuyerName(existingSale.buyerName ?? '');
      setBuyerPhone(existingSale.buyerPhone ?? '');
      setBuyerEmail(existingSale.buyerEmail ?? '');
      setFinalSoldPrice(String(existingSale.finalSoldPrice));
      setSoldAtUtc(toLocalDateTimeValue(existingSale.soldAtUtc));
      setPaymentMethod(existingSale.paymentMethod ?? '');
      setNotes(existingSale.notes ?? '');
      setSelectedCustomer(null);
    } else {
      setBuyerName('');
      setBuyerPhone('');
      setBuyerEmail('');
      setFinalSoldPrice(String(productPrice));
      setSoldAtUtc(toLocalDateTimeValue(null));
      setPaymentMethod('');
      setNotes('');
      setSelectedCustomer(null);
    }
    setCustomerSearch('');
  }, [open, existingSale, productPrice]);

  const saveMutation = useMutation({
    mutationFn: async (input: SaveProductSaleInput) => {
      if (isUpdate) {
        return updateProductSale(productId!, input);
      }
      return createProductSale(productId!, input);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      await queryClient.invalidateQueries({ queryKey: ['product-sale', productId] });
      // Also invalidate customer queries so newly created/linked customers appear immediately
      await queryClient.invalidateQueries({ queryKey: ['admin-customers'] });
      onSaved();
    },
  });

  const handleSubmit = () => {
    const price = Number(finalSoldPrice);
    if (Number.isNaN(price) || price < 0) return;

    const input: SaveProductSaleInput = {
      finalSoldPrice: price,
      soldAtUtc: localDateTimeToUtcIso(soldAtUtc),
      customerId: selectedCustomer?.id ?? (existingSale?.customerId || null),
      buyerName: buyerName.trim() || null,
      buyerPhone: buyerPhone.trim() || null,
      buyerEmail: buyerEmail.trim() || null,
      paymentMethod: paymentMethod || null,
      notes: notes.trim() || null,
    };

    saveMutation.mutate(input);
  };

  const priceError = (() => {
    const v = finalSoldPrice.trim();
    if (v === '') return 'Required';
    const n = Number(v);
    if (Number.isNaN(n)) return 'Must be a number';
    if (n < 0) return 'Cannot be negative';
    return null;
  })();

  const canSubmit = !priceError && soldAtUtc.trim() !== '' && !saveMutation.isPending;

  const hasBuyerContact = buyerEmail.trim() !== '' || buyerPhone.trim() !== '';

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      slotProps={{ paper: { sx: { maxHeight: '90vh' } } }}
    >
      <DialogTitle>
        {isUpdate ? 'Edit Sale Info' : 'Mark as Sold'}
        <Typography variant="body2" color="text.secondary">
          {productTitle}
        </Typography>
      </DialogTitle>

      <DialogContent dividers>
        {saleQuery.isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {saveMutation.isError && (
              <Alert severity="error">
                {resolveErrorMessage(saveMutation.error, 'Failed to save sale record.')}
              </Alert>
            )}

            {/* Price & Date row */}
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField
                label="Final Sold Price"
                type="number"
                value={finalSoldPrice}
                onChange={(e) => setFinalSoldPrice(e.target.value)}
                error={Boolean(priceError && finalSoldPrice !== '')}
                helperText={
                  finalSoldPrice !== '' ? priceError : `Listed price: $${productPrice}`
                }
                inputProps={{ min: 0, step: '0.01' }}
                fullWidth
                required
              />
              <TextField
                label="Sold At"
                type="datetime-local"
                value={soldAtUtc}
                onChange={(e) => setSoldAtUtc(e.target.value)}
                fullWidth
                required
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Stack>

            {/* Buyer info */}
            <Stack spacing={0.5}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                Buyer Information
              </Typography>
              <Typography variant="caption" color="text.disabled">
                Providing email or phone will automatically create or link a customer record.
              </Typography>
            </Stack>
            <TextField
              label="Buyer Name"
              value={buyerName}
              onChange={(e) => setBuyerName(e.target.value)}
              inputProps={{ maxLength: 200 }}
              fullWidth
            />
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField
                label="Buyer Phone"
                value={buyerPhone}
                onChange={(e) => setBuyerPhone(e.target.value)}
                inputProps={{ maxLength: 40 }}
                fullWidth
              />
              <TextField
                label="Buyer Email"
                value={buyerEmail}
                onChange={(e) => setBuyerEmail(e.target.value)}
                inputProps={{ maxLength: 256 }}
                fullWidth
              />
            </Stack>

            {hasBuyerContact && !selectedCustomer && !existingSale?.customerId && (
              <Alert severity="info" sx={{ py: 0.5 }}>
                A customer record will be automatically created or matched from the buyer info above.
              </Alert>
            )}

            {/* Linked Customer */}
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Link to Existing Customer (Optional)
            </Typography>
            <Autocomplete
              options={customerSearchQuery.data?.items ?? []}
              getOptionLabel={(option) =>
                [option.name, option.email, option.phone].filter(Boolean).join(' — ')
              }
              getOptionKey={(option) => option.id}
              value={selectedCustomer}
              onChange={(_, value) => setSelectedCustomer(value)}
              onInputChange={(_, value) => setCustomerSearch(value)}
              loading={customerSearchQuery.isFetching}
              noOptionsText={customerSearch.length < 2 ? 'Type at least 2 characters' : 'No customers found'}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Linked Customer"
                  placeholder="Search by name, email or phone..."
                  helperText={
                    !selectedCustomer && existingSale?.customerId
                      ? `Currently linked: ${existingSale.customerId}`
                      : 'If left empty and buyer email/phone is provided, a customer will be auto-resolved.'
                  }
                />
              )}
              isOptionEqualToValue={(option, value) => option.id === value.id}
            />

            {/* Payment & Notes */}
            <Select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              displayEmpty
              fullWidth
              size="small"
            >
              <MenuItem value="">
                <Typography color="text.secondary">Payment Method (optional)</Typography>
              </MenuItem>
              {paymentMethodOptions.map((m) => (
                <MenuItem key={m} value={m}>
                  {paymentMethodLabels[m]}
                </MenuItem>
              ))}
            </Select>

            <TextField
              label="Notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              multiline
              minRows={2}
              maxRows={4}
              inputProps={{ maxLength: 2000 }}
              fullWidth
            />
          </Stack>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saveMutation.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={!canSubmit}
        >
          {saveMutation.isPending ? 'Saving...' : isUpdate ? 'Update Sale' : 'Mark as Sold'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
