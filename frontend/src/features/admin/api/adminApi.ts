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

export interface ProductImageUploadUrlResult {
  uploadUrl: string;
  objectKey: string;
  publicUrl: string;
  expiresAtUtc: string;
}

export interface AddProductImageInput {
  objectKey: string;
  url: string;
  altText?: string;
  sortOrder: number;
  isPrimary: boolean;
}

export async function createProductImageUploadUrl(
  productId: string,
  fileName: string,
  contentType: string,
): Promise<ProductImageUploadUrlResult> {
  const response = await httpClient.post<ProductImageUploadUrlResult>(
    `/api/admin/products/${productId}/images/presigned-url`,
    { fileName, contentType },
  );
  return response.data;
}

export async function uploadImageToR2(uploadUrl: string, file: File): Promise<void> {
  const response = await fetch(uploadUrl, {
    method: 'PUT',
    headers: {
      'Content-Type': file.type || 'application/octet-stream',
    },
    body: file,
  });

  if (!response.ok) {
    throw new Error(`Failed to upload image to object storage: ${response.status}`);
  }
}

export async function addProductImage(productId: string, input: AddProductImageInput): Promise<void> {
  await httpClient.post(`/api/admin/products/${productId}/images`, input);
}
