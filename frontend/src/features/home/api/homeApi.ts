import type { ProductListItem } from '../../../entities/product/types';
import { httpClient } from '../../../shared/api/httpClient';

export async function fetchFeaturedProducts(limit = 8): Promise<ProductListItem[]> {
  const response = await httpClient.get<ProductListItem[]>('/api/products/featured', {
    params: { limit },
  });
  return response.data;
}
