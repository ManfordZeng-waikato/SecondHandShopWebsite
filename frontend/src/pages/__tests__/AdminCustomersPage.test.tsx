import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError } from 'axios';
import { AdminCustomersPage } from '../AdminCustomersPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import {
  createAdminCustomer,
  fetchAdminCustomerDetail,
  fetchAdminCustomers,
  updateAdminCustomer,
} from '../../features/admin/api/adminApi';

vi.mock('../../features/admin/api/adminApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/admin/api/adminApi')>('../../features/admin/api/adminApi');
  return {
    ...actual,
    createAdminCustomer: vi.fn(),
    fetchAdminCustomerDetail: vi.fn(),
    fetchAdminCustomers: vi.fn(),
    updateAdminCustomer: vi.fn(),
  };
});

describe('AdminCustomersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchAdminCustomers).mockResolvedValue({
      items: [
        {
          id: 'customer-1',
          name: 'Alice',
          email: 'alice@example.com',
          phone: '021 123 4567',
          status: 'New',
          primarySource: 'Inquiry',
          inquiryCount: 2,
          lastInquiryAt: '2026-04-16T01:00:00Z',
          purchaseCount: 0,
          totalSpent: 0,
          lastPurchaseAtUtc: null,
          lastContactAtUtc: '2026-04-16T01:00:00Z',
          createdAt: '2026-04-15T01:00:00Z',
          updatedAt: '2026-04-16T01:00:00Z',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
      isFallback: false,
    });
    vi.mocked(createAdminCustomer).mockResolvedValue({ id: 'customer-2' });
    vi.mocked(fetchAdminCustomerDetail).mockResolvedValue({
      id: 'customer-1',
      name: 'Alice',
      email: 'alice@example.com',
      phone: '021 123 4567',
      status: 'New',
      primarySource: 'Inquiry',
      notes: 'VIP',
      inquiryCount: 2,
      lastInquiryAt: '2026-04-16T01:00:00Z',
      purchaseCount: 0,
      totalSpent: 0,
      lastPurchaseAtUtc: null,
      lastContactAtUtc: '2026-04-16T01:00:00Z',
      createdAt: '2026-04-15T01:00:00Z',
      updatedAt: '2026-04-16T01:00:00Z',
    });
    vi.mocked(updateAdminCustomer).mockResolvedValue();
  });

  it('creates a customer from the add customer dialog', async () => {
    renderWithProviders(<AdminCustomersPage />);

    await userEvent.click(await screen.findByRole('button', { name: /add customer/i }));
    await userEvent.type(screen.getByLabelText(/^email$/i), 'new@example.com');
    await userEvent.click(screen.getByRole('button', { name: /^create$/i }));

    await waitFor(() => {
      expect(createAdminCustomer).toHaveBeenCalledWith({
        name: undefined,
        email: 'new@example.com',
        phoneNumber: undefined,
        notes: undefined,
      });
    });
  });

  it('shows conflict guidance when create customer returns a duplicate contact conflict', async () => {
    vi.mocked(createAdminCustomer).mockRejectedValue(
      new AxiosError('Conflict', 'ERR_BAD_REQUEST', undefined, undefined, {
        status: 409,
        statusText: 'Conflict',
        headers: {},
        config: {} as never,
        data: {
          existingCustomerId: 'customer-1',
          conflictField: 'email',
          message: 'A customer with this email already exists.',
        },
      }),
    );

    renderWithProviders(<AdminCustomersPage />);

    await userEvent.click(await screen.findByRole('button', { name: /add customer/i }));
    await userEvent.type(screen.getByLabelText(/^email$/i), 'alice@example.com');
    await userEvent.click(screen.getByRole('button', { name: /^create$/i }));

    expect(await screen.findByText(/a customer with this email already exists/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /open existing customer/i })).toHaveAttribute('href', '/lord/customers/customer-1');
  });

  it('loads customer details and saves edits from the edit dialog', async () => {
    renderWithProviders(<AdminCustomersPage />);

    await userEvent.click(await screen.findByRole('button', { name: /^edit$/i }));
    const nameInput = await screen.findByLabelText(/^name$/i);
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, 'Alice Updated');
    await userEvent.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(updateAdminCustomer).toHaveBeenCalledWith('customer-1', {
        name: 'Alice Updated',
        phoneNumber: '021 123 4567',
        notes: 'VIP',
      });
    });
  });
});
