import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { ProductsPage } from '../ProductsPage';
import { fetchCategories, fetchProductsPaged } from '../../features/catalog/api/catalogApi';
import type { Category } from '../../entities/category/types';
import type { PagedResult, ProductListItem } from '../../entities/product/types';

vi.mock('../../features/catalog/api/catalogApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/catalog/api/catalogApi')>(
    '../../features/catalog/api/catalogApi',
  );
  return {
    ...actual,
    fetchCategories: vi.fn(),
    fetchProductsPaged: vi.fn(),
  };
});

function renderProductsPage(initialEntry = '/products') {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <QueryClientProvider client={queryClient}>
        <ProductsPage />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

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

function pagedResult(overrides: Partial<PagedResult<ProductListItem>> = {}): PagedResult<ProductListItem> {
  return {
    items: [product()],
    page: 1,
    pageSize: 24,
    totalCount: 1,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
    isFallback: false,
    ...overrides,
  };
}

const categories: Category[] = [
  {
    id: 'category-1',
    name: 'Furniture',
    slug: 'furniture',
    sortOrder: 0,
    isActive: true,
  },
];

describe('ProductsPage', () => {
  beforeEach(() => {
    Element.prototype.scrollIntoView = vi.fn();
    vi.mocked(fetchCategories).mockResolvedValue(categories);
    vi.mocked(fetchProductsPaged).mockResolvedValue(pagedResult());
  });

  it('loads products using search and price filters from the URL', async () => {
    renderProductsPage('/products?search=oak&minPrice=50&maxPrice=200');

    expect(await screen.findByText('Oak side table')).toBeInTheDocument();
    await waitFor(() => {
      expect(fetchProductsPaged).toHaveBeenCalledWith(
        expect.objectContaining({
          search: 'oak',
          minPrice: 50,
          maxPrice: 200,
        }),
        expect.any(AbortSignal),
      );
    });
    expect(screen.getByText('oak')).toBeInTheDocument();
  });

  it('requests the selected category when a category tab is clicked', async () => {
    renderProductsPage();

    await screen.findByText('Oak side table');
    await userEvent.click(await screen.findByRole('tab', { name: 'Furniture' }));

    await waitFor(() => {
      expect(fetchProductsPaged).toHaveBeenLastCalledWith(
        expect.objectContaining({ category: 'furniture' }),
        expect.any(AbortSignal),
      );
    });
  });

  it('requests the next page when pagination changes', async () => {
    vi.mocked(fetchProductsPaged).mockResolvedValue(
      pagedResult({
        totalCount: 48,
        totalPages: 2,
        hasNextPage: true,
      }),
    );

    renderProductsPage();

    await screen.findByText('Oak side table');
    await userEvent.click(await screen.findByRole('button', { name: /go to page 2/i }));

    await waitFor(() => {
      expect(fetchProductsPaged).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2 }),
        expect.any(AbortSignal),
      );
    });
  });

  it('shows the empty state when no products match', async () => {
    vi.mocked(fetchProductsPaged).mockResolvedValue(pagedResult({ items: [], totalCount: 0, totalPages: 0 }));

    renderProductsPage();

    expect(await screen.findByText(/no products found/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear all filters/i })).toBeEnabled();
  });

  it('shows an error state when products fail to load', async () => {
    vi.mocked(fetchProductsPaged).mockRejectedValue(new Error('offline'));

    renderProductsPage();

    expect(await screen.findByText(/failed to load products/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /retry/i })).toBeEnabled();
  });
});
