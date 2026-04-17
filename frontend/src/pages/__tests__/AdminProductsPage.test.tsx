import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AdminProductsPage } from '../AdminProductsPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import {
  fetchAdminProducts,
  updateProductFeatured,
  updateProductStatus,
} from '../../features/admin/api/adminApi';
import { fetchCategoryTree } from '../../features/catalog/api/catalogApi';

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useLocation: () => ({ pathname: '/lord/products', state: null }),
    useNavigate: () => vi.fn(),
  };
});

vi.mock('../../features/admin/api/adminApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/admin/api/adminApi')>('../../features/admin/api/adminApi');
  return {
    ...actual,
    fetchAdminProducts: vi.fn(),
    updateProductFeatured: vi.fn(),
    updateProductStatus: vi.fn(),
  };
});

vi.mock('../../features/catalog/api/catalogApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/catalog/api/catalogApi')>('../../features/catalog/api/catalogApi');
  return {
    ...actual,
    fetchCategoryTree: vi.fn(),
  };
});

vi.mock('../../features/admin/components/ProductSaleDialog', () => ({
  ProductSaleDialog: () => null,
}));
vi.mock('../../features/admin/components/RevertSaleDialog', () => ({
  RevertSaleDialog: () => null,
}));
vi.mock('../../features/admin/components/ProductSaleHistoryDialog', () => ({
  ProductSaleHistoryDialog: () => null,
}));
vi.mock('../../features/admin/components/ProductCategoryDialog', () => ({
  ProductCategoryDialog: () => null,
}));

describe('AdminProductsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchCategoryTree).mockResolvedValue([]);
    vi.mocked(fetchAdminProducts).mockResolvedValue({
      items: [
        {
          id: 'product-1',
          title: 'Vintage bag',
          slug: 'vintage-bag',
          price: 250,
          status: 'Available',
          categoryName: 'Bags',
          imageCount: 1,
          primaryImageUrl: null,
          isFeatured: false,
          featuredSortOrder: null,
          createdAt: '2026-04-16T01:00:00Z',
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
    vi.mocked(updateProductStatus).mockResolvedValue();
    vi.mocked(updateProductFeatured).mockResolvedValue();
  });

  it('saves featured settings for an available product', async () => {
    renderWithProviders(<AdminProductsPage />);

    await screen.findByText('Vintage bag');
    const toggle = screen.getByRole('switch');
    const sortOrderInput = screen.getByLabelText(/sort order/i);
    const saveButton = screen.getByRole('button', { name: /^save$/i });

    await userEvent.click(toggle);
    await waitFor(() => {
      expect(sortOrderInput).toBeEnabled();
      expect(saveButton).toBeEnabled();
    });
    await userEvent.type(sortOrderInput, '5');
    await userEvent.click(saveButton);

    await waitFor(() => {
      expect(updateProductFeatured).toHaveBeenCalledWith('product-1', {
        isFeatured: true,
        featuredSortOrder: 5,
      });
    });
  });
});
