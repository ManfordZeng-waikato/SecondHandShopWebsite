import { httpClient } from '../../../../shared/api/httpClient';
import { fetchFeaturedProducts } from '../homeApi';

vi.mock('../../../../shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
  },
}));

describe('homeApi', () => {
  it('fetches featured products using the default limit', async () => {
    vi.mocked(httpClient.get).mockResolvedValue({ data: [{ id: 'product-1' }] });

    await expect(fetchFeaturedProducts()).resolves.toEqual([{ id: 'product-1' }]);

    expect(httpClient.get).toHaveBeenCalledWith('/api/products/featured', {
      params: { limit: 8 },
    });
  });

  it('fetches featured products using an explicit limit', async () => {
    vi.mocked(httpClient.get).mockResolvedValue({ data: [] });

    await fetchFeaturedProducts(3);

    expect(httpClient.get).toHaveBeenCalledWith('/api/products/featured', {
      params: { limit: 3 },
    });
  });
});
