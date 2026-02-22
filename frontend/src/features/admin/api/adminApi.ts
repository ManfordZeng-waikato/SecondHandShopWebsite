import type { CreateProductInput, ProductStatus } from '../../../entities/product/types';
import { httpClient } from '../../../shared/api/httpClient';
import { env } from '../../../shared/config/env';
import { createMockProduct, updateMockProductStatus } from '../../../shared/mock/mockApi';

export async function createProduct(input: CreateProductInput): Promise<{ id: string }> {
  if (env.useMockApi) {
    return createMockProduct(input);
  }

  const response = await httpClient.post<{ id: string }>('/api/admin/products', input);
  return response.data;
}

export async function updateProductStatus(productId: string, status: ProductStatus): Promise<void> {
  if (env.useMockApi) {
    return updateMockProductStatus(productId, status);
  }

  await httpClient.put(`/api/admin/products/${productId}/status`, { status });
}
