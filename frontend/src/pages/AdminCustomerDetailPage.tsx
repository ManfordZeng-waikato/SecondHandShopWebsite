import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Typography,
} from '@mui/material';
import { Link as RouterLink, useParams } from 'react-router-dom';
import type { AxiosError } from 'axios';
import type { EditableCustomer, UpdateCustomerInput, CustomerSource } from '../entities/customer/types';
import {
  fetchAdminCustomerDetail,
  fetchAdminCustomerInquiries,
  fetchCustomerSales,
  updateAdminCustomer,
} from '../features/admin/api/adminApi';
import { CustomerEditDialog } from '../features/admin/components/CustomerEditDialog';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

function parseApiDate(value: string): Date {
  const hasTimeZone = /([zZ]|[+-]\d{2}:\d{2})$/.test(value);
  return new Date(hasTimeZone ? value : `${value}Z`);
}

function formatDateTime(value: string | null): string {
  if (!value) {
    return '—';
  }

  const parsed = parseApiDate(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString('en-NZ', {
    timeZone: 'Pacific/Auckland',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatCurrency(value: number): string {
  return `$${value.toFixed(2)}`;
}

function resolveErrorMessage(error: unknown, fallbackMessage: string): string {
  const axiosError = error as AxiosError<{ message?: string }>;
  const responseMessage = axiosError.response?.data?.message;
  if (typeof responseMessage === 'string' && responseMessage.trim().length > 0) {
    return responseMessage;
  }

  return fallbackMessage;
}

const sourceColorMap: Record<CustomerSource, { color: string; bg: string }> = {
  Inquiry: { color: '#1565c0', bg: 'rgba(21,101,192,0.08)' },
  Sale: { color: '#2e7d32', bg: 'rgba(46,125,50,0.08)' },
  Manual: { color: '#757575', bg: 'rgba(117,117,117,0.08)' },
};

export function AdminCustomerDetailPage() {
  const { customerId } = useParams<{ customerId: string }>();
  const queryClient = useQueryClient();
  const [inquiryPage, setInquiryPage] = useState(1);
  const [inquiryPageSize, setInquiryPageSize] = useState(20);
  const [editTarget, setEditTarget] = useState<EditableCustomer | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ severity: 'success' | 'error'; message: string } | null>(null);

  const customerQuery = useQuery({
    queryKey: ['admin-customer', customerId],
    enabled: Boolean(customerId),
    queryFn: () => fetchAdminCustomerDetail(customerId!),
  });

  const inquiryQuery = useQuery({
    queryKey: ['admin-customer-inquiries', customerId, inquiryPage, inquiryPageSize],
    enabled: Boolean(customerId),
    queryFn: () =>
      fetchAdminCustomerInquiries(customerId!, {
        page: inquiryPage,
        pageSize: inquiryPageSize,
      }),
  });

  const salesQuery = useQuery({
    queryKey: ['admin-customer-sales', customerId],
    enabled: Boolean(customerId),
    queryFn: () => fetchCustomerSales(customerId!),
  });

  const updateCustomerMutation = useMutation({
    mutationFn: ({ targetCustomerId, input }: { targetCustomerId: string; input: UpdateCustomerInput }) =>
      updateAdminCustomer(targetCustomerId, input),
    onSuccess: async (_, variables) => {
      setSaveError(null);
      setEditTarget(null);
      setFeedback({ severity: 'success', message: 'Customer updated successfully.' });
      await queryClient.invalidateQueries({ queryKey: ['admin-customers'] });
      await queryClient.invalidateQueries({ queryKey: ['admin-customer', variables.targetCustomerId] });
    },
    onError: (error) => {
      const message = resolveErrorMessage(error, 'Failed to update customer.');
      setSaveError(message);
      setFeedback({ severity: 'error', message });
    },
  });

  if (!customerId) {
    return <Alert severity="error">Invalid customer id.</Alert>;
  }

  if (customerQuery.isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (customerQuery.isError || !customerQuery.data) {
    return (
      <Stack spacing={2}>
        <Alert severity="error">Unable to load customer details.</Alert>
        <Box>
          <Button variant="outlined" component={RouterLink} to="/lord/customers">
            Back to customers
          </Button>
        </Box>
      </Stack>
    );
  }

  const customer = customerQuery.data;
  const inquiries = inquiryQuery.data?.items ?? [];
  const inquiryTotalCount = inquiryQuery.data?.totalCount ?? 0;
  const sourceStyle = sourceColorMap[customer.primarySource];

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
        <Typography variant="h4">Customer detail</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" component={RouterLink} to="/lord/customers">
            Back
          </Button>
          <Button
            variant="contained"
            onClick={() => {
              setSaveError(null);
              setEditTarget({
                id: customer.id,
                name: customer.name ?? '',
                email: customer.email ?? '',
                phone: customer.phone ?? '',
                notes: customer.notes ?? '',
              });
            }}
          >
            Edit
          </Button>
        </Stack>
      </Stack>

      {/* Basic information */}
      <Paper sx={{ p: 2.5 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
          <Typography variant="h6">Basic information</Typography>
          <Chip
            label={customer.primarySource}
            size="small"
            sx={{
              fontWeight: 600,
              fontSize: '0.65rem',
              height: 22,
              color: sourceStyle.color,
              bgcolor: sourceStyle.bg,
              border: 'none',
            }}
          />
        </Stack>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
            gap: 2,
          }}
        >
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Name</Typography>
            <Typography>{customer.name || '—'}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Email</Typography>
            <Typography>{customer.email || '—'}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Phone</Typography>
            <Typography>{customer.phone || '—'}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Status</Typography>
            <Typography>{customer.status}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Notes</Typography>
            <Typography>{customer.notes || '—'}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Last contact</Typography>
            <Typography>{formatDateTime(customer.lastContactAtUtc)}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Created at</Typography>
            <Typography>{formatDateTime(customer.createdAt)}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary">Updated at</Typography>
            <Typography>{formatDateTime(customer.updatedAt)}</Typography>
          </Stack>
        </Box>
      </Paper>

      {/* Summary stats */}
      <Paper sx={{ p: 2.5 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>Summary</Typography>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' },
            gap: 2,
          }}
        >
          <Box sx={{ textAlign: 'center', py: 1 }}>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>{customer.inquiryCount}</Typography>
            <Typography variant="caption" color="text.secondary">Inquiries</Typography>
          </Box>
          <Box sx={{ textAlign: 'center', py: 1 }}>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>{customer.purchaseCount}</Typography>
            <Typography variant="caption" color="text.secondary">Purchases</Typography>
          </Box>
          <Box sx={{ textAlign: 'center', py: 1 }}>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>{formatCurrency(customer.totalSpent)}</Typography>
            <Typography variant="caption" color="text.secondary">Total Spent</Typography>
          </Box>
          <Box sx={{ textAlign: 'center', py: 1 }}>
            <Typography variant="body1" sx={{ fontWeight: 600 }}>
              {formatDateTime(customer.lastPurchaseAtUtc)}
            </Typography>
            <Typography variant="caption" color="text.secondary">Last Purchase</Typography>
          </Box>
        </Box>
      </Paper>

      {/* Purchase History */}
      <Paper>
        <Box sx={{ p: 2.5, pb: 1 }}>
          <Typography variant="h6">Purchase history</Typography>
        </Box>
        {salesQuery.isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : salesQuery.isError ? (
          <Box sx={{ px: 2.5, pb: 2.5 }}>
            <Alert severity="error">Unable to load purchase history.</Alert>
          </Box>
        ) : (
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Product</TableCell>
                  <TableCell>Sold Price</TableCell>
                  <TableCell>Payment</TableCell>
                  <TableCell>Sold At</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {(salesQuery.data ?? []).length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} align="center">
                      <Typography color="text.secondary" sx={{ py: 2 }}>
                        No purchase records for this customer.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  (salesQuery.data ?? []).map((sale) => (
                    <TableRow key={sale.saleId} hover>
                      <TableCell>
                        {sale.productSlug ? (
                          <Button
                            component={RouterLink}
                            to={`/products/${sale.productSlug}`}
                            variant="text"
                            size="small"
                            sx={{ px: 0, justifyContent: 'flex-start' }}
                          >
                            {sale.productTitle}
                          </Button>
                        ) : (
                          <Typography color="text.secondary">{sale.productTitle}</Typography>
                        )}
                      </TableCell>
                      <TableCell>{formatCurrency(sale.finalSoldPrice)}</TableCell>
                      <TableCell>{sale.paymentMethod ?? '—'}</TableCell>
                      <TableCell>{formatDateTime(sale.soldAtUtc)}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Paper>

      {/* Inquiry History */}
      <Paper>
        <Box sx={{ p: 2.5, pb: 1 }}>
          <Typography variant="h6">Inquiry history</Typography>
        </Box>
        {inquiryQuery.isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : inquiryQuery.isError ? (
          <Box sx={{ px: 2.5, pb: 2.5 }}>
            <Alert severity="error">Unable to load inquiries for this customer.</Alert>
          </Box>
        ) : (
          <>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Inquiry Id</TableCell>
                    <TableCell>Product</TableCell>
                    <TableCell>Message</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Created at</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {inquiries.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        <Typography color="text.secondary" sx={{ py: 2 }}>
                          No inquiries found for this customer.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    inquiries.map((inquiry) => (
                      <TableRow key={inquiry.inquiryId} hover>
                        <TableCell sx={{ fontFamily: 'monospace' }}>{inquiry.inquiryId}</TableCell>
                        <TableCell>
                          <Stack spacing={0.25}>
                            {inquiry.productSlug ? (
                              <Button
                                component={RouterLink}
                                to={`/products/${inquiry.productSlug}`}
                                variant="text"
                                size="small"
                                sx={{ px: 0, justifyContent: 'flex-start' }}
                              >
                                {inquiry.productTitle || 'View product'}
                              </Button>
                            ) : (
                              <Typography color="text.secondary">
                                {inquiry.productTitle || 'Product unavailable'}
                              </Typography>
                            )}
                            <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                              {inquiry.productId}
                            </Typography>
                          </Stack>
                        </TableCell>
                        <TableCell sx={{ maxWidth: 420 }}>{inquiry.message}</TableCell>
                        <TableCell>{inquiry.inquiryStatus}</TableCell>
                        <TableCell>{formatDateTime(inquiry.createdAt)}</TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              component="div"
              count={inquiryTotalCount}
              page={Math.max(0, inquiryPage - 1)}
              onPageChange={(_, nextPage) => setInquiryPage(nextPage + 1)}
              rowsPerPage={inquiryPageSize}
              onRowsPerPageChange={(event) => {
                setInquiryPageSize(Number(event.target.value));
                setInquiryPage(1);
              }}
              rowsPerPageOptions={PAGE_SIZE_OPTIONS}
            />
          </>
        )}
      </Paper>

      <CustomerEditDialog
        open={Boolean(editTarget)}
        customer={editTarget}
        isSubmitting={updateCustomerMutation.isPending}
        errorMessage={saveError}
        onClose={() => {
          setSaveError(null);
          setEditTarget(null);
        }}
        onSubmit={async (input) => {
          if (!editTarget) {
            return;
          }

          await updateCustomerMutation.mutateAsync({
            targetCustomerId: editTarget.id,
            input,
          });
        }}
      />

      <Snackbar
        open={Boolean(feedback)}
        autoHideDuration={3000}
        onClose={() => setFeedback(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {feedback ? (
          <Alert onClose={() => setFeedback(null)} severity={feedback.severity} sx={{ width: '100%' }}>
            {feedback.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </Stack>
  );
}
