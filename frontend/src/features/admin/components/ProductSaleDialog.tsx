/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
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
import type { MarkProductSoldInput, ProductInquiryOption } from '../../../entities/sale/types';
import { paymentMethodOptions, paymentMethodLabels } from '../../../entities/sale/types';
import type { CustomerListItem } from '../../../entities/customer/types';
import { fetchAdminCustomers, fetchProductInquiries, markProductSold } from '../api/adminApi';

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

function formatInquiryOptionLabel(option: ProductInquiryOption): string {
  const contact = option.customerName || option.email || option.phoneNumber || 'Unknown contact';
  const createdAt = new Date(option.createdAt).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
  return `${contact} • ${createdAt}`;
}

function formatInquiryHelper(option: ProductInquiryOption | null): string {
  if (!option) {
    return 'Link the sale to a specific inquiry so cohort analytics can attribute the conversion.';
  }

  const message = option.message.trim();
  if (message.length <= 120) {
    return message;
  }

  return `${message.slice(0, 117)}...`;
}

/**
 * Dialog for recording a new sale on an Available or OffShelf product. Each submission
 * creates a new ProductSale history row — this dialog never edits an existing sale
 * (history is immutable; use the revert flow and then mark sold again if a correction
 * is needed).
 */
export function ProductSaleDialog({
  open,
  productId,
  productTitle,
  productPrice,
  onClose,
  onSaved,
}: ProductSaleDialogProps) {
  const queryClient = useQueryClient();

  const [buyerName, setBuyerName] = useState('');
  const [buyerPhone, setBuyerPhone] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [finalSoldPrice, setFinalSoldPrice] = useState('');
  const [soldAtUtc, setSoldAtUtc] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [notes, setNotes] = useState('');
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerListItem | null>(null);
  const [customerSearch, setCustomerSearch] = useState('');
  const [selectedInquiry, setSelectedInquiry] = useState<ProductInquiryOption | null>(null);

  const customerSearchQuery = useQuery({
    queryKey: ['admin-customers-search', customerSearch],
    queryFn: () => fetchAdminCustomers({ search: customerSearch, pageSize: 10 }),
    enabled: open && customerSearch.length >= 2,
    staleTime: 30_000,
  });

  const productInquiriesQuery = useQuery({
    queryKey: ['product-inquiries', productId],
    queryFn: () => fetchProductInquiries(productId!),
    enabled: open && Boolean(productId),
    staleTime: 30_000,
  });

  useEffect(() => {
    if (!open) return;

    setBuyerName('');
    setBuyerPhone('');
    setBuyerEmail('');
    setFinalSoldPrice(String(productPrice));
    setSoldAtUtc(toLocalDateTimeValue(null));
    setPaymentMethod('');
    setNotes('');
    setSelectedCustomer(null);
    setCustomerSearch('');
    setSelectedInquiry(null);
  }, [open, productPrice]);

  const saveMutation = useMutation({
    mutationFn: async (input: MarkProductSoldInput) => {
      if (!productId) throw new Error('Missing product id.');
      return markProductSold(productId, input);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      await queryClient.invalidateQueries({ queryKey: ['product-sale', productId] });
      await queryClient.invalidateQueries({ queryKey: ['product-sale-history', productId] });
      await queryClient.invalidateQueries({ queryKey: ['product-inquiries', productId] });
      await queryClient.invalidateQueries({ queryKey: ['admin-customers'] });
      await queryClient.invalidateQueries({ queryKey: ['admin', 'analytics'] });
      onSaved();
    },
  });

  const handleSubmit = () => {
    const price = Number(finalSoldPrice);
    if (Number.isNaN(price) || price < 0) return;

    const input: MarkProductSoldInput = {
      finalSoldPrice: price,
      soldAtUtc: localDateTimeToUtcIso(soldAtUtc),
      customerId: selectedCustomer?.id ?? null,
      inquiryId: selectedInquiry?.inquiryId ?? null,
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
  const hasProductInquiries = (productInquiriesQuery.data?.length ?? 0) > 0;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      slotProps={{ paper: { sx: { maxHeight: '90vh' } } }}
    >
      <DialogTitle>
        Mark as Sold
        <Typography variant="body2" color="text.secondary">
          {productTitle}
        </Typography>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          {saveMutation.isError && (
            <Alert severity="error">
              {resolveErrorMessage(saveMutation.error, 'Failed to record sale.')}
            </Alert>
          )}

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

          <Stack spacing={0.5}>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}
            >
              Inquiry Attribution
            </Typography>
            <Typography variant="caption" color="text.disabled">
              Select the inquiry that led to this sale so cohort conversion analytics can count it.
            </Typography>
          </Stack>
          <Autocomplete
            options={productInquiriesQuery.data ?? []}
            getOptionLabel={formatInquiryOptionLabel}
            value={selectedInquiry}
            onChange={(_, value) => setSelectedInquiry(value)}
            loading={productInquiriesQuery.isFetching}
            noOptionsText={
              productInquiriesQuery.isLoading
                ? 'Loading inquiries...'
                : 'No inquiries found for this product'
            }
            isOptionEqualToValue={(option, value) => option.inquiryId === value.inquiryId}
            getOptionDisabled={(option) => option.linkedSaleId !== null}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Linked Inquiry"
                placeholder={hasProductInquiries ? 'Select an inquiry' : 'No inquiries available'}
                helperText={formatInquiryHelper(selectedInquiry)}
              />
            )}
            renderOption={(props, option) => (
              <Box component="li" {...props}>
                <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {formatInquiryOptionLabel(option)}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>
                    {option.message}
                  </Typography>
                  {option.linkedSaleId ? (
                    <Typography variant="caption" color="warning.main">
                      Already linked to sale {option.linkedSaleId}
                    </Typography>
                  ) : null}
                </Stack>
              </Box>
            )}
          />

          <Stack spacing={0.5}>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}
            >
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

          {hasBuyerContact && !selectedCustomer && (
            <Alert severity="info" sx={{ py: 0.5 }}>
              A customer record will be automatically created or matched from the buyer info above.
            </Alert>
          )}

          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}
          >
            Link to Existing Customer (Optional)
          </Typography>
          <Autocomplete
            options={customerSearchQuery.data?.items ?? []}
            getOptionLabel={(option) =>
              [option.name, option.email, option.phone].filter(Boolean).join(' • ')
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
              />
            )}
            isOptionEqualToValue={(option, value) => option.id === value.id}
          />

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

          <Box sx={{ bgcolor: 'action.hover', borderRadius: 1, p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              Each submission creates a new sale record. Once recorded, buyer/price/time fields
              cannot be edited — to make corrections, revert the sale first.
            </Typography>
          </Box>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saveMutation.isPending}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="error"
          onClick={handleSubmit}
          disabled={!canSubmit}
        >
          {saveMutation.isPending ? 'Saving...' : 'Mark as Sold'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
