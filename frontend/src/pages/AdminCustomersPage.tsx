import { type FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Fade,
  Paper,
  Select,
  MenuItem,
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
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import SearchIcon from '@mui/icons-material/Search';
import FilterListIcon from '@mui/icons-material/FilterList';
import PeopleOutlineIcon from '@mui/icons-material/PeopleOutline';
import type { AxiosError } from 'axios';
import { customerStatusOptions } from '../entities/customer/types';
import type {
  CustomerStatus,
  CustomerListItem,
  CustomerSortBy,
  EditableCustomer,
  SortDirection,
  UpdateCustomerInput,
  CustomerSource,
} from '../entities/customer/types';
import {
  fetchAdminCustomerDetail,
  fetchAdminCustomers,
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
    return '\u2014';
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

const filterChipSx = (isActive: boolean) => ({
  fontWeight: isActive ? 700 : 500,
  fontSize: '0.8rem',
  bgcolor: isActive ? 'primary.main' : 'transparent',
  color: isActive ? 'primary.contrastText' : 'text.secondary',
  borderColor: isActive ? 'primary.main' : 'divider',
  '&:hover': {
    bgcolor: isActive ? 'primary.dark' : 'action.hover',
  },
  transition: 'all 0.15s ease',
}) as const;

const statusColorMap: Record<CustomerStatus, { color: string; bg: string }> = {
  New: { color: '#1565c0', bg: 'rgba(21,101,192,0.08)' },
  Contacted: { color: '#e65100', bg: 'rgba(230,81,0,0.08)' },
  Qualified: { color: '#2e7d32', bg: 'rgba(46,125,50,0.08)' },
  Archived: { color: '#757575', bg: 'rgba(117,117,117,0.08)' },
};

const sourceColorMap: Record<CustomerSource, { color: string; bg: string }> = {
  Inquiry: { color: '#1565c0', bg: 'rgba(21,101,192,0.08)' },
  Sale: { color: '#2e7d32', bg: 'rgba(46,125,50,0.08)' },
  Manual: { color: '#757575', bg: 'rgba(117,117,117,0.08)' },
};

export function AdminCustomersPage() {
  const queryClient = useQueryClient();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  // Search
  const [searchInput, setSearchInput] = useState('');
  const [searchKeyword, setSearchKeyword] = useState('');

  // Filters
  const [statusFilter, setStatusFilter] = useState<CustomerStatus | 'all'>('all');

  // Sorting
  const [sortBy, setSortBy] = useState<CustomerSortBy>('createdAt');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  // Pagination
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // Edit
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

  const resetFilters = () => {
    setSearchInput('');
    setSearchKeyword('');
    setStatusFilter('all');
    setPage(1);
  };

  const customers = customersQuery.data?.items ?? [];
  const totalCount = customersQuery.data?.totalCount ?? 0;
  const hasActiveFilters = searchKeyword || statusFilter !== 'all';

  const openEditDialog = async (customer: CustomerListItem) => {
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
  };

  // --- Page header ---
  const headerSection = (
    <Box sx={{ mb: 1 }}>
      <Typography variant="h4" sx={{ lineHeight: 1.2 }}>
        Customer Management
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
        {customersQuery.isLoading
          ? 'Loading...'
          : `${totalCount} ${totalCount === 1 ? 'customer' : 'customers'} total`}
        {hasActiveFilters ? ' (filtered)' : ''}
      </Typography>
    </Box>
  );

  // --- Search & sort toolbar ---
  const searchSection = (
    <Paper sx={{ p: 2 }}>
      <Stack
        component="form"
        onSubmit={handleSearch}
        direction={{ xs: 'column', md: 'row' }}
        spacing={1.5}
        alignItems={{ xs: 'stretch', md: 'center' }}
      >
        <TextField
          size="small"
          label="Search by name, email, or phone"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          slotProps={{
            input: {
              startAdornment: <SearchIcon sx={{ fontSize: 18, color: 'text.disabled', mr: 0.5 }} />,
            },
          }}
          sx={{ flex: 1 }}
        />
        <Select
          size="small"
          value={sortBy}
          onChange={(event) => {
            setSortBy(event.target.value as CustomerSortBy);
            setPage(1);
          }}
          sx={{ minWidth: 190 }}
        >
          <MenuItem value="createdAt">Sort: date created</MenuItem>
          <MenuItem value="updatedAt">Sort: last updated</MenuItem>
          <MenuItem value="lastInquiryAt">Sort: latest inquiry</MenuItem>
          <MenuItem value="totalSpent">Sort: total spent</MenuItem>
          <MenuItem value="lastPurchaseAt">Sort: last purchase</MenuItem>
        </Select>
        <Select
          size="small"
          value={sortDirection}
          onChange={(event) => {
            setSortDirection(event.target.value as SortDirection);
            setPage(1);
          }}
          sx={{ minWidth: 100 }}
        >
          <MenuItem value="desc">Desc</MenuItem>
          <MenuItem value="asc">Asc</MenuItem>
        </Select>
        <Button type="submit" variant="contained" size="small">
          Search
        </Button>
        {hasActiveFilters && (
          <Button variant="outlined" size="small" onClick={resetFilters}>
            Reset
          </Button>
        )}
      </Stack>
    </Paper>
  );

  // --- Filter toolbar ---
  const filterSection = (
    <Paper sx={{ p: 2.5 }}>
      <Stack direction="row" spacing={0.75} alignItems="center" sx={{ mb: 1.25 }}>
        <FilterListIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
          Status
        </Typography>
      </Stack>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
        <Chip
          label="All"
          size="small"
          variant={statusFilter === 'all' ? 'filled' : 'outlined'}
          onClick={() => { setStatusFilter('all'); setPage(1); }}
          sx={filterChipSx(statusFilter === 'all')}
        />
        {customerStatusOptions.map((status) => (
          <Chip
            key={status}
            label={status}
            size="small"
            variant={statusFilter === status ? 'filled' : 'outlined'}
            onClick={() => { setStatusFilter(statusFilter === status ? 'all' : status); setPage(1); }}
            sx={filterChipSx(statusFilter === status)}
          />
        ))}
      </Box>
    </Paper>
  );

  // --- Loading state ---
  if (customersQuery.isLoading) {
    return (
      <Stack spacing={2.5}>
        {headerSection}
        {searchSection}
        {filterSection}
        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', py: 8 }}>
          <CircularProgress size={32} sx={{ mb: 1.5, color: 'text.secondary' }} />
          <Typography variant="body2" color="text.secondary">
            Loading customers...
          </Typography>
        </Box>
      </Stack>
    );
  }

  // --- Error state ---
  if (customersQuery.isError) {
    return (
      <Stack spacing={2.5}>
        {headerSection}
        {searchSection}
        {filterSection}
        <Alert severity="error">Unable to load customers. Please refresh and try again.</Alert>
      </Stack>
    );
  }

  // --- Empty state ---
  if (customers.length === 0) {
    return (
      <Stack spacing={2.5}>
        {headerSection}
        {searchSection}
        {filterSection}
        <Paper sx={{ py: 8, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <PeopleOutlineIcon sx={{ fontSize: 48, color: 'grey.300', mb: 2 }} />
          <Typography variant="h6" color="text.secondary" sx={{ mb: 0.5 }}>
            No customers found
          </Typography>
          <Typography variant="body2" color="text.disabled">
            {hasActiveFilters
              ? 'Try adjusting the search or filters above.'
              : 'Customers will appear here once they submit inquiries or make purchases.'}
          </Typography>
        </Paper>

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

  // --- Mobile card layout ---
  const mobileList = (
    <Stack spacing={1.5}>
      {customers.map((customer, index) => {
        const statusStyle = statusColorMap[customer.status];
        const sourceStyle = sourceColorMap[customer.primarySource];
        return (
          <Fade in timeout={Math.min(300, 150 + index * 20)} key={customer.id}>
            <Paper
              sx={{
                p: 2.5,
                transition: 'border-color 0.15s ease',
                '&:hover': { borderColor: '#c0c0c0' },
              }}
            >
              <Stack spacing={1.5}>
                {/* Name + Status + Source */}
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="h6" sx={{ fontSize: '1rem' }} noWrap>
                    {customer.name || '\u2014'}
                  </Typography>
                  <Stack direction="row" spacing={0.5}>
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
                    <Chip
                      label={customer.status}
                      size="small"
                      sx={{
                        fontWeight: 600,
                        fontSize: '0.7rem',
                        height: 24,
                        color: statusStyle.color,
                        bgcolor: statusStyle.bg,
                        border: 'none',
                      }}
                    />
                  </Stack>
                </Stack>

                {/* Contact info */}
                <Stack spacing={0.5}>
                  {customer.email && (
                    <Typography variant="body2" color="text.secondary" noWrap>
                      {customer.email}
                    </Typography>
                  )}
                  {customer.phone && (
                    <Typography variant="body2" color="text.secondary">
                      {customer.phone}
                    </Typography>
                  )}
                </Stack>

                <Divider sx={{ borderColor: '#f0f0f0' }} />

                {/* Meta row */}
                <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
                  <Typography variant="caption" color="text.secondary">
                    {customer.inquiryCount} {customer.inquiryCount === 1 ? 'inquiry' : 'inquiries'}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {customer.purchaseCount} {customer.purchaseCount === 1 ? 'purchase' : 'purchases'}
                  </Typography>
                  {customer.totalSpent > 0 && (
                    <Typography variant="caption" sx={{ fontWeight: 600 }}>
                      {formatCurrency(customer.totalSpent)} spent
                    </Typography>
                  )}
                </Stack>
                <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
                  <Typography variant="caption" color="text.disabled">
                    Last purchase: {formatDateTime(customer.lastPurchaseAtUtc)}
                  </Typography>
                  <Typography variant="caption" color="text.disabled">
                    Created: {formatDateTime(customer.createdAt)}
                  </Typography>
                </Stack>

                {/* Actions */}
                <Stack direction="row" spacing={1}>
                  <Button
                    size="small"
                    variant="outlined"
                    component={RouterLink}
                    to={`/lord/customers/${customer.id}`}
                    fullWidth
                  >
                    View
                  </Button>
                  <Button
                    size="small"
                    variant="contained"
                    fullWidth
                    onClick={() => openEditDialog(customer)}
                  >
                    Edit
                  </Button>
                </Stack>
              </Stack>
            </Paper>
          </Fade>
        );
      })}
    </Stack>
  );

  // --- Desktop table layout ---
  const desktopTable = (
    <Paper sx={{ overflow: 'hidden' }}>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Phone</TableCell>
              <TableCell>Source</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Inquiries</TableCell>
              <TableCell align="right">Purchases</TableCell>
              <TableCell align="right">Total Spent</TableCell>
              <TableCell>Last Purchase</TableCell>
              <TableCell>Created</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {customers.map((customer) => {
              const statusStyle = statusColorMap[customer.status];
              const sourceStyle = sourceColorMap[customer.primarySource];
              return (
                <TableRow key={customer.id} hover>
                  <TableCell>{customer.name || '\u2014'}</TableCell>
                  <TableCell>{customer.email || '\u2014'}</TableCell>
                  <TableCell>{customer.phone || '\u2014'}</TableCell>
                  <TableCell>
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
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={customer.status}
                      size="small"
                      sx={{
                        fontWeight: 600,
                        fontSize: '0.7rem',
                        height: 24,
                        color: statusStyle.color,
                        bgcolor: statusStyle.bg,
                        border: 'none',
                      }}
                    />
                  </TableCell>
                  <TableCell align="right">{customer.inquiryCount}</TableCell>
                  <TableCell align="right">{customer.purchaseCount}</TableCell>
                  <TableCell align="right">
                    {customer.totalSpent > 0 ? formatCurrency(customer.totalSpent) : '\u2014'}
                  </TableCell>
                  <TableCell>{formatDateTime(customer.lastPurchaseAtUtc)}</TableCell>
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
                        onClick={() => openEditDialog(customer)}
                      >
                        Edit
                      </Button>
                    </Stack>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );

  // --- Main render ---
  return (
    <Stack spacing={2.5}>
      {headerSection}
      {searchSection}
      {filterSection}

      {/* Pagination top */}
      <Paper sx={{ overflow: 'hidden' }}>
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

      {isMobile ? mobileList : desktopTable}

      {/* Pagination bottom */}
      <Paper sx={{ overflow: 'hidden' }}>
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
