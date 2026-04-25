import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { ProductDetailPage } from '../ProductDetailPage';
import { fetchProductBySlug } from '../../features/catalog/api/catalogApi';
import type { Product } from '../../entities/product/types';

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ slug: 'oak-side-table' }),
  };
});

vi.mock('../../features/catalog/api/catalogApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/catalog/api/catalogApi')>(
    '../../features/catalog/api/catalogApi',
  );
  return {
    ...actual,
    fetchProductBySlug: vi.fn(),
  };
});

function renderDetailPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <ProductDetailPage />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

function product(overrides: Partial<Product> = {}): Product {
  return {
    id: 'product-1',
    title: 'Oak side table',
    slug: 'oak-side-table',
    description: 'Solid oak with a tidy drawer.',
    price: 120,
    status: 'Available',
    categoryId: 'category-1',
    categoryName: 'Furniture',
    images: [
      {
        id: 'image-1',
        objectKey: 'front.jpg',
        displayUrl: 'https://images.example.test/front.jpg',
        altText: 'Front view',
        sortOrder: 0,
        isPrimary: true,
      },
      {
        id: 'image-2',
        objectKey: 'side.jpg',
        displayUrl: 'https://images.example.test/side.jpg',
        altText: 'Side view',
        sortOrder: 1,
        isPrimary: false,
      },
    ],
    createdAt: '2026-04-20T00:00:00Z',
    updatedAt: '2026-04-20T00:00:00Z',
    ...overrides,
  };
}

describe('ProductDetailPage', () => {
  beforeEach(() => {
    vi.mocked(fetchProductBySlug).mockResolvedValue(product());
  });

  it('switches the main image when a thumbnail is selected', async () => {
    renderDetailPage();

    expect(await screen.findByRole('heading', { name: 'Oak side table' })).toBeInTheDocument();
    await userEvent.click(screen.getAllByAltText('Side view')[0]);

    expect(screen.getAllByAltText('Side view')[0]).toHaveAttribute(
      'src',
      'https://images.example.test/side.jpg',
    );
  });

  it('links available products to the inquiry page', async () => {
    renderDetailPage();

    const link = await screen.findByRole('link', { name: /send inquiry/i });

    expect(link).toHaveAttribute('href', '/products/product-1/inquiry');
  });

  it('does not show the inquiry entrance for sold products', async () => {
    vi.mocked(fetchProductBySlug).mockResolvedValue(product({ status: 'Sold' }));

    renderDetailPage();

    expect(await screen.findByRole('button', { name: /item sold/i })).toBeDisabled();
    expect(screen.queryByRole('link', { name: /send inquiry/i })).not.toBeInTheDocument();
  });
});
