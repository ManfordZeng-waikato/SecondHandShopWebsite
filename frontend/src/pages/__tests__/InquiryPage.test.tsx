import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { forwardRef, useImperativeHandle } from 'react';
import { InquiryPage } from '../InquiryPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { fetchProductById } from '../../features/catalog/api/catalogApi';
import { createInquiry } from '../../features/inquiry/api/inquiryApi';

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ id: 'product-1' }),
  };
});

vi.mock('../../features/catalog/api/catalogApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/catalog/api/catalogApi')>('../../features/catalog/api/catalogApi');
  return {
    ...actual,
    fetchProductById: vi.fn(),
  };
});

vi.mock('../../features/inquiry/api/inquiryApi', () => ({
  createInquiry: vi.fn(),
}));

vi.mock('../../features/inquiry/components/TurnstileWidget', () => ({
  TurnstileWidget: forwardRef(function MockTurnstileWidget(
    { onVerify }: { onVerify: (token: string) => void },
    ref,
  ) {
    useImperativeHandle(ref, () => ({
      execute: () => true,
      reset: () => undefined,
    }));

    return (
      <button type="button" onClick={() => onVerify('turnstile-ok')}>
        Verify security
      </button>
    );
  }),
}));

describe('InquiryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchProductById).mockResolvedValue({
      id: 'product-1',
      title: 'Vintage bag',
      slug: 'vintage-bag',
      description: 'Soft grain leather bag.',
      price: 250,
      status: 'Available',
      categoryId: 'category-1',
      categoryName: 'Bags',
      images: [],
      createdAt: '2026-04-16T01:00:00Z',
      updatedAt: '2026-04-16T01:00:00Z',
    });
    vi.mocked(createInquiry).mockResolvedValue({ inquiryId: 'inq-1' });
  });

  it('blocks submission when no contact method is provided', async () => {
    renderWithProviders(<InquiryPage />);

    await screen.findByText('Vintage bag');
    await userEvent.type(screen.getByLabelText(/message/i), 'Is this still available?');
    await userEvent.click(screen.getByRole('button', { name: /send inquiry/i }));

    expect(await screen.findByText(/please provide at least one contact method/i)).toBeInTheDocument();
  });

  it('submits the inquiry and shows a success confirmation after verification', async () => {
    renderWithProviders(<InquiryPage />);

    await screen.findByText('Vintage bag');
    await userEvent.type(screen.getByLabelText(/your name/i), 'Alice');
    await userEvent.type(screen.getByLabelText(/email/i), 'alice@example.com');
    await userEvent.type(screen.getByLabelText(/message/i), 'Is this still available?');
    await userEvent.click(screen.getByRole('button', { name: /verify security/i }));
    await userEvent.click(screen.getByRole('button', { name: /send inquiry/i }));

    await waitFor(() => {
      expect(createInquiry).toHaveBeenCalled();
    });
    expect(vi.mocked(createInquiry).mock.calls[0]?.[0]).toEqual({
      productId: 'product-1',
      customerName: 'Alice',
      email: 'alice@example.com',
      phoneNumber: undefined,
      message: 'Is this still available?',
      turnstileToken: 'turnstile-ok',
    });

    expect(await screen.findByText(/inquiry sent!/i)).toBeInTheDocument();
  });
});
