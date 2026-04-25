import { screen, waitFor } from '@testing-library/react';
import { HomePage } from '../HomePage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { fetchFeaturedProducts } from '../../features/home/api/homeApi';
import type { ProductListItem } from '../../entities/product/types';

vi.mock('../../features/home/api/homeApi', () => ({
  fetchFeaturedProducts: vi.fn(),
}));

function product(overrides: Partial<ProductListItem> = {}): ProductListItem {
  return {
    id: 'product-1',
    title: 'Oak side table',
    slug: 'oak-side-table',
    price: 120,
    coverImageUrl: 'https://images.example.test/oak-side-table.jpg',
    categoryName: 'Furniture',
    status: 'Available',
    createdAt: '2026-04-20T00:00:00Z',
    ...overrides,
  };
}

describe('HomePage', () => {
  beforeEach(() => {
    vi.mocked(fetchFeaturedProducts).mockResolvedValue([product()]);
  });

  it('renders featured products after loading them', async () => {
    renderWithProviders(<HomePage />);

    expect(await screen.findByText('Oak side table')).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /view all products/i })[0]).toHaveAttribute(
      'href',
      '/products',
    );
  });

  it('hides the featured section when there are no featured products', async () => {
    vi.mocked(fetchFeaturedProducts).mockResolvedValue([]);

    renderWithProviders(<HomePage />);

    expect(await screen.findByText(/our story/i)).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.queryByText('Trending Now')).not.toBeInTheDocument();
    });
  });

  it('shows an error state when featured products fail to load', async () => {
    vi.mocked(fetchFeaturedProducts).mockRejectedValue(new Error('offline'));

    renderWithProviders(<HomePage />);

    expect(await screen.findByText(/failed to load featured products/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /retry/i })).toBeEnabled();
  });
});
