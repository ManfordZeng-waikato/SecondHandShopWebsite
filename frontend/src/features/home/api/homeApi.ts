import type { ProductListItem } from '../../../entities/product/types';
import { httpClient } from '../../../shared/api/httpClient';
import { env } from '../../../shared/config/env';
import { getMockFeaturedProducts } from '../../../shared/mock/mockApi';

export async function fetchFeaturedProducts(limit = 8): Promise<ProductListItem[]> {
  if (env.useMockApi) {
    return getMockFeaturedProducts(limit);
  }

  const response = await httpClient.get<ProductListItem[]>('/api/products/featured', {
    params: { limit },
  });
  return response.data;
}
