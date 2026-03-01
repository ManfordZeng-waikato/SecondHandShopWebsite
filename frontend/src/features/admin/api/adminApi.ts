import type { CreateProductInput, ProductStatus } from '../../../entities/product/types';
import { httpClient } from '../../../shared/api/httpClient';
import { env } from '../../../shared/config/env';
import { createMockProduct, getMockProductsForAdmin, updateMockProductStatus } from '../../../shared/mock/mockApi';

export interface AdminProductListItem {
  id: string;
  title: string;
  slug: string;
  price: number;
  condition: string;
  status: ProductStatus;
  categoryName?: string;
  imageCount: number;
  primaryImageUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export async function fetchAdminProducts(status?: ProductStatus): Promise<AdminProductListItem[]> {
  if (env.useMockApi) {
    return getMockProductsForAdmin(status);
  }

  const response = await httpClient.get<AdminProductListItem[]>('/api/admin/products', {
    params: status ? { status } : undefined,
  });
  return response.data;
}

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

export interface PresignedUploadResult {
  objectKey: string;
  putUrl: string;
  expiresInSeconds: number;
}

export interface AddProductImageInput {
  objectKey: string;
  altText?: string;
  sortOrder: number;
  isPrimary: boolean;
}

export async function createProductImageUploadUrl(
  productId: string,
  fileName: string,
  contentType: string,
): Promise<PresignedUploadResult> {
  const response = await httpClient.post<PresignedUploadResult>(
    `/api/admin/products/${productId}/images/presigned-url`,
    { fileName, contentType },
  );
  return response.data;
}

const MAX_UPLOAD_RETRIES = 2;

export async function uploadImageToR2(putUrl: string, file: File): Promise<void> {
  let lastError: Error | undefined;

  for (let attempt = 0; attempt <= MAX_UPLOAD_RETRIES; attempt++) {
    try {
      const response = await fetch(putUrl, {
        method: 'PUT',
        headers: { 'Content-Type': file.type || 'application/octet-stream' },
        body: file,
      });

      if (response.ok) return;

      if (response.status >= 400 && response.status < 500) {
        throw new Error(`Upload rejected (${response.status}). The presigned URL may have expired.`);
      }

      lastError = new Error(`Upload failed with status ${response.status}`);
    } catch (err) {
      if (err instanceof Error && err.message.includes('rejected')) throw err;
      lastError = err instanceof Error ? err : new Error(String(err));
    }
  }

  throw lastError ?? new Error('Upload failed after retries');
}

export async function addProductImage(productId: string, input: AddProductImageInput): Promise<void> {
  await httpClient.post(`/api/admin/products/${productId}/images`, input);
}

export async function deleteProductImage(productId: string, imageId: string): Promise<void> {
  await httpClient.delete(`/api/admin/products/${productId}/images/${imageId}`);
}

export async function removeBackgroundPreview(file: File): Promise<Blob> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await httpClient.post<Blob>(
    '/api/admin/images/remove-background-preview',
    formData,
    {
      responseType: 'blob',
      timeout: 60_000,
      headers: { 'Content-Type': 'multipart/form-data' },
    },
  );
  return response.data;
}

export async function uploadBlobToR2(putUrl: string, blob: Blob, contentType: string): Promise<void> {
  let lastError: Error | undefined;

  for (let attempt = 0; attempt <= MAX_UPLOAD_RETRIES; attempt++) {
    try {
      const response = await fetch(putUrl, {
        method: 'PUT',
        headers: { 'Content-Type': contentType },
        body: blob,
      });

      if (response.ok) return;

      if (response.status >= 400 && response.status < 500) {
        throw new Error(`Upload rejected (${response.status}). The presigned URL may have expired.`);
      }

      lastError = new Error(`Upload failed with status ${response.status}`);
    } catch (err) {
      if (err instanceof Error && err.message.includes('rejected')) throw err;
      lastError = err instanceof Error ? err : new Error(String(err));
    }
  }

  throw lastError ?? new Error('Upload failed after retries');
}
