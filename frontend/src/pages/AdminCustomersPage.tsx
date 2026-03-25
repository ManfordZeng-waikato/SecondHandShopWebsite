import { type FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import type { AxiosError } from 'axios';
import { customerStatusOptions } from '../entities/customer/types';
import type {
  CustomerStatus,
  CustomerListItem,
  CustomerSortBy,
  EditableCustomer,
  SortDirection,
  UpdateCustomerInput,
} from '../entities/customer/types';
import {
  fetchAdminCustomerDetail,
  fetchAdminCustomers,
  updateAdminCustomer,
} from '../features/admin/api/adminApi';
import { CustomerEditDialog } from '../features/admin/components/CustomerEditDialog';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

function parseApiDate(value: string): Date {
  // Backend DateTime payload may omit timezone suffix (e.g. "2026-03-25T02:23:00").
  // Treat timezone-less values as UTC to avoid local-time misinterpretation.
  const hasTimeZone = /([zZ]|[+\-]\d{2}:\d{2})$/.test(value);
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

function resolveErrorMessage(error: unknown, fallbackMessage: string): string {
  const axiosError = error as AxiosError<{ message?: string }>;
  const responseMessage = axiosError.response?.data?.message;
  if (typeof responseMessage === 'string' && responseMessage.trim().length > 0) {
    return responseMessage;
  }

  return fallbackMessage;
}

function toEditableCustomer(customer: CustomerListItem): EditableCustomer {
  return {
    id: customer.id,
    name: customer.name ?? '',
    email: customer.email ?? '',
    phone: customer.phone ?? '',
    status: customer.status,
    notes: '',
  };
}

export function AdminCustomersPage() {
  const queryClient = useQueryClient();
  const [searchInput, setSearchInput] = useState('');
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<CustomerStatus | 'all'>('all');
  const [sortBy, setSortBy] = useState<CustomerSortBy>('createdAt');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [editTarget, setEditTarget] = useState<EditableCustomer | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ severity: 'success' | 'error'; message: string } | null>(null);

  const customersQuery = useQuery({
    queryKey: ['admin-customers', page, pageSize, searchKeyword, statusFilter, sortBy, sortDirection],
    queryFn: () =>
      fetchAdminCustomers({
        page,
        pageSize,
        search: searchKeyword || undefined,
        status: statusFilter === 'all' ? undefined : statusFilter,
        sortBy,
        sortDirection,
      }),
  });

  const updateCustomerMutation = useMutation({
    mutationFn: ({ customerId, input }: { customerId: string; input: UpdateCustomerInput }) =>
      updateAdminCustomer(customerId, input),
    onSuccess: async () => {
      setSaveError(null);
      setEditTarget(null);
      setFeedback({ severity: 'success', message: 'Customer updated successfully.' });
      await queryClient.invalidateQueries({ queryKey: ['admin-customers'] });
      await queryClient.invalidateQueries({ queryKey: ['admin-customer'] });
    },
    onError: (error) => {
      const message = resolveErrorMessage(error, 'Failed to update customer.');
      setSaveError(message);
      setFeedback({ severity: 'error', message });
    },
  });

  const handleSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPage(1);
    setSearchKeyword(searchInput.trim());
  };

  const customers = customersQuery.data?.items ?? [];
  const totalCount = customersQuery.data?.totalCount ?? 0;

  return (
    <Stack spacing={2}>
      <Typography variant="h4">Customer management</Typography>

      <Paper sx={{ p: 2 }}>
        <Stack
          component="form"
          onSubmit={handleSearch}
          direction={{ xs: 'column', md: 'row' }}
          spacing={1.5}
          alignItems={{ xs: 'stretch', md: 'center' }}
        >
          <TextField
            label="Search by name, email, or phone"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            fullWidth
          />
          <Select
            size="small"
            value={statusFilter}
            onChange={(event) => {
              setStatusFilter(event.target.value as CustomerStatus | 'all');
              setPage(1);
            }}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="all">All status</MenuItem>
            {customerStatusOptions.map((statusOption) => (
              <MenuItem key={statusOption} value={statusOption}>
                {statusOption}
              </MenuItem>
            ))}
          </Select>
          <Select
            size="small"
            value={sortBy}
            onChange={(event) => {
              setSortBy(event.target.value as CustomerSortBy);
              setPage(1);
            }}
            sx={{ minWidth: 190 }}
          >
            <MenuItem value="createdAt">Sort: newest created</MenuItem>
            <MenuItem value="updatedAt">Sort: newest updated</MenuItem>
            <MenuItem value="lastInquiryAt">Sort: latest inquiry</MenuItem>
          </Select>
          <Select
            size="small"
            value={sortDirection}
            onChange={(event) => {
              setSortDirection(event.target.value as SortDirection);
              setPage(1);
            }}
            sx={{ minWidth: 120 }}
          >
            <MenuItem value="desc">Desc</MenuItem>
            <MenuItem value="asc">Asc</MenuItem>
          </Select>
          <Button type="submit" variant="contained">
            Search
          </Button>
          <Button
            variant="outlined"
            onClick={() => {
              setSearchInput('');
              setSearchKeyword('');
              setStatusFilter('all');
              setPage(1);
            }}
          >
            Reset
          </Button>
        </Stack>
      </Paper>

      {customersQuery.isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      ) : customersQuery.isError ? (
        <Alert severity="error">Unable to load customers. Please refresh and try again.</Alert>
      ) : (
        <Paper>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Phone</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Inquiry count</TableCell>
                  <TableCell>Last inquiry</TableCell>
                  <TableCell>Created at</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {customers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center">
                      <Typography color="text.secondary" sx={{ py: 2 }}>
                        No customers found.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  customers.map((customer) => (
                    <TableRow key={customer.id} hover>
                      <TableCell>{customer.name || '—'}</TableCell>
                      <TableCell>{customer.email || '—'}</TableCell>
                      <TableCell>{customer.phone || '—'}</TableCell>
                      <TableCell>{customer.status}</TableCell>
                      <TableCell align="right">{customer.inquiryCount}</TableCell>
                      <TableCell>{formatDateTime(customer.lastInquiryAt)}</TableCell>
                      <TableCell>{formatDateTime(customer.createdAt)}</TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={1} justifyContent="flex-end">
                          <Button
                            size="small"
                            variant="outlined"
                            component={RouterLink}
                            to={`/lord/customers/${customer.id}`}
                          >
                            View
                          </Button>
                          <Button
                            size="small"
                            variant="contained"
                            onClick={async () => {
                              setSaveError(null);
                              try {
                                const detail = await queryClient.fetchQuery({
                                  queryKey: ['admin-customer', customer.id],
                                  queryFn: () => fetchAdminCustomerDetail(customer.id),
                                });

                                setEditTarget({
                                  ...toEditableCustomer(customer),
                                  status: detail.status,
                                  notes: detail.notes ?? '',
                                });
                              } catch {
                                setFeedback({
                                  severity: 'error',
                                  message: 'Unable to load customer details for editing.',
                                });
                              }
                            }}
                          >
                            Edit
                          </Button>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={totalCount}
            page={Math.max(0, page - 1)}
            onPageChange={(_, nextPage) => setPage(nextPage + 1)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(1);
            }}
            rowsPerPageOptions={PAGE_SIZE_OPTIONS}
          />
        </Paper>
      )}

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
            customerId: editTarget.id,
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
