import { httpClient } from '../../../../shared/api/httpClient';
import {
  fetchCategories,
  fetchCategoryTree,
  fetchProductById,
  fetchProductBySlug,
  fetchProductsPaged,
} from '../catalogApi';

vi.mock('../../../../shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
  },
}));

describe('catalogApi', () => {
  it('fetches categories and category tree from public endpoints', async () => {
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [{ id: 'cat-1' }] });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [{ id: 'root', children: [] }] });

    await expect(fetchCategories()).resolves.toEqual([{ id: 'cat-1' }]);
    await expect(fetchCategoryTree()).resolves.toEqual([{ id: 'root', children: [] }]);

    expect(httpClient.get).toHaveBeenNthCalledWith(1, '/api/categories');
    expect(httpClient.get).toHaveBeenNthCalledWith(2, '/api/categories/tree');
  });

  it('fetches paged products with params and abort signal', async () => {
    const result = {
      items: [],
      page: 1,
      pageSize: 12,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
      isFallback: false,
    };
    const signal = new AbortController().signal;
    vi.mocked(httpClient.get).mockResolvedValue({ data: result });

    await expect(fetchProductsPaged({ search: 'oak', minPrice: 10 }, signal)).resolves.toBe(result);

    expect(httpClient.get).toHaveBeenCalledWith('/api/products/search', {
      params: { search: 'oak', minPrice: 10 },
      signal,
    });
  });

  it('returns product data for id and slug lookups', async () => {
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { id: 'product-1' } });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { slug: 'oak-chair' } });

    await expect(fetchProductById('product-1')).resolves.toEqual({ id: 'product-1' });
    await expect(fetchProductBySlug('oak-chair')).resolves.toEqual({ slug: 'oak-chair' });

    expect(httpClient.get).toHaveBeenNthCalledWith(1, '/api/products/product-1');
    expect(httpClient.get).toHaveBeenNthCalledWith(2, '/api/products/slug/oak-chair');
  });

  it('returns null when product detail lookups fail', async () => {
    vi.mocked(httpClient.get).mockRejectedValue(new Error('not found'));

    await expect(fetchProductById('missing')).resolves.toBeNull();
    await expect(fetchProductBySlug('missing')).resolves.toBeNull();
  });
});
