import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ProductSaleDialog } from '../ProductSaleDialog';
import { renderWithProviders } from '../../../../test/renderWithProviders';
import {
  fetchAdminCustomers,
  fetchProductInquiries,
  markProductSold,
} from '../../api/adminApi';

vi.mock('../../api/adminApi', () => ({
  fetchAdminCustomers: vi.fn(),
  fetchProductInquiries: vi.fn(),
  markProductSold: vi.fn(),
}));

const mockedFetchAdminCustomers = vi.mocked(fetchAdminCustomers);
const mockedFetchProductInquiries = vi.mocked(fetchProductInquiries);
const mockedMarkProductSold = vi.mocked(markProductSold);

describe('ProductSaleDialog', () => {
  beforeEach(() => {
    mockedFetchAdminCustomers.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
      isFallback: false,
    });
    mockedFetchProductInquiries.mockResolvedValue([]);
    mockedMarkProductSold.mockResolvedValue({
      id: 'sale-1',
      productId: 'product-1',
      customerId: null,
      inquiryId: null,
      listedPriceAtSale: 250,
      finalSoldPrice: 250,
      buyerName: null,
      buyerPhone: null,
      buyerEmail: null,
      soldAtUtc: new Date().toISOString(),
      paymentMethod: null,
      notes: null,
      status: 'Completed',
      cancelledAtUtc: null,
      cancellationReason: null,
      cancellationNote: null,
      createdByAdminUserId: null,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    });
  });

  it('prefills listed price and blocks submission when the sold price is negative', async () => {
    renderWithProviders(
      <ProductSaleDialog
        open
        productId="product-1"
        productTitle="Vintage bag"
        productPrice={250}
        onClose={vi.fn()}
        onSaved={vi.fn()}
      />,
    );

    const priceInput = await screen.findByLabelText(/final sold price/i);
    const saveButton = screen.getByRole('button', { name: /mark as sold/i });

    expect(priceInput).toHaveValue(250);
    await userEvent.clear(priceInput);
    await userEvent.type(priceInput, '-1');

    expect(screen.getByText(/cannot be negative/i)).toBeInTheDocument();
    expect(saveButton).toBeDisabled();
  });

  it('shows customer auto-link hint when buyer contact is entered without selecting a customer', async () => {
    renderWithProviders(
      <ProductSaleDialog
        open
        productId="product-1"
        productTitle="Vintage bag"
        productPrice={250}
        onClose={vi.fn()}
        onSaved={vi.fn()}
      />,
    );

    await userEvent.type(await screen.findByLabelText(/buyer email/i), 'alice@example.com');

    expect(
      screen.getByText(/customer record will be automatically created or matched/i),
    ).toBeInTheDocument();
  });

  it('submits trimmed buyer details and calls onSaved after a successful mutation', async () => {
    const onSaved = vi.fn();

    renderWithProviders(
      <ProductSaleDialog
        open
        productId="product-1"
        productTitle="Vintage bag"
        productPrice={250}
        onClose={vi.fn()}
        onSaved={onSaved}
      />,
    );

    await userEvent.type(await screen.findByLabelText(/buyer name/i), '  Alice  ');
    await userEvent.type(screen.getByLabelText(/buyer email/i), 'alice@example.com');
    await userEvent.click(screen.getByRole('button', { name: /mark as sold/i }));

    await waitFor(() => {
      expect(mockedMarkProductSold).toHaveBeenCalledWith(
        'product-1',
        expect.objectContaining({
          finalSoldPrice: 250,
          buyerName: 'Alice',
          buyerEmail: 'alice@example.com',
        }),
      );
    });
    await waitFor(() => {
      expect(onSaved).toHaveBeenCalledTimes(1);
    });
  });
});
