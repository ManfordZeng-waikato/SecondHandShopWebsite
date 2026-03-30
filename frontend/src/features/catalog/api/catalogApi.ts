import type { Category } from '../../../entities/category/types';
import type {
  PagedResult,
  Product,
  ProductListItem,
  ProductQueryParams,
} from '../../../entities/product/types';
import { httpClient } from '../../../shared/api/httpClient';

export async function fetchCategories(): Promise<Category[]> {
  const response = await httpClient.get<Category[]>('/api/categories');
  return response.data;
}

export async function fetchProductsPaged(
  params: ProductQueryParams,
  signal?: AbortSignal,
): Promise<PagedResult<ProductListItem>> {
  const response = await httpClient.get<PagedResult<ProductListItem>>(
    '/api/products/search',
    { params, signal },
  );
  return response.data;
}

export async function fetchProductById(id: string): Promise<Product | null> {
  try {
    const response = await httpClient.get<Product>(`/api/products/${id}`);
    return response.data;
  } catch {
    return null;
  }
}

export async function fetchProductBySlug(slug: string): Promise<Product | null> {
  try {
    const response = await httpClient.get<Product>(`/api/products/slug/${slug}`);
    return response.data;
  } catch {
    return null;
  }
}
