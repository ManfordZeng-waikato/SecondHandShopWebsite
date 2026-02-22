import type { Category } from '../../../entities/category/types';
import type { Product } from '../../../entities/product/types';
import { httpClient } from '../../../shared/api/httpClient';
import { env } from '../../../shared/config/env';
import { getMockCategories, getMockProductBySlug, getMockProducts } from '../../../shared/mock/mockApi';

export async function fetchCategories(): Promise<Category[]> {
  if (env.useMockApi) {
    return getMockCategories();
  }

  const response = await httpClient.get<Category[]>('/api/categories');
  return response.data;
}

export async function fetchProducts(categoryId?: string): Promise<Product[]> {
  if (env.useMockApi) {
    return getMockProducts(categoryId);
  }

  const response = await httpClient.get<Product[]>('/api/products', {
    params: categoryId ? { categoryId } : undefined,
  });
  return response.data;
}

export async function fetchProductBySlug(slug: string): Promise<Product | null> {
  if (env.useMockApi) {
    return getMockProductBySlug(slug);
  }

  try {
    const response = await httpClient.get<Product>(`/api/products/slug/${slug}`);
    return response.data;
  } catch {
    return null;
  }
}
