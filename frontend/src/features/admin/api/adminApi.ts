import type {
  PagedResult,
  CreateProductInput,
  ProductStatus,
} from '../../../entities/product/types';
import type {
  AdminCustomerQueryParams,
  CreateCustomerInput,
  CreateCustomerResult,
  CustomerDetail,
  CustomerInquiryItem,
  CustomerInquiryQueryParams,
  CustomerListItem,
  UpdateCustomerInput,
} from '../../../entities/customer/types';
import type {
  ProductSaleDto,
  MarkProductSoldInput,
  RevertProductSaleInput,
  CustomerSaleItem,
} from '../../../entities/sale/types';
import { httpClient } from '../../../shared/api/httpClient';

export interface LoginResponse {
  expiresAt: string;
  requiresPasswordChange: boolean;
}

export async function loginAdmin(userName: string, password: string): Promise<LoginResponse> {
  const response = await httpClient.post<LoginResponse>('/api/lord/auth/login', {
    userName,
    password,
  });
  return response.data;
}

export interface ChangeInitialPasswordInput {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangeInitialPasswordResponse {
  success: boolean;
  requiresReLogin: boolean;
  message: string;
}

export async function changeAdminInitialPassword(
  input: ChangeInitialPasswordInput,
): Promise<ChangeInitialPasswordResponse> {
  const response = await httpClient.post<ChangeInitialPasswordResponse>(
    '/api/lord/auth/change-initial-password',
    {
      currentPassword: input.currentPassword,
      newPassword: input.newPassword,
      confirmNewPassword: input.confirmNewPassword,
    },
  );
  return response.data;
}

export async function logoutAdmin(): Promise<void> {
  await httpClient.post('/api/lord/auth/logout');
}

/** Database-backed session info; prefer initializeAdminAuth() for bootstrap to avoid axios cycles. */
export interface AdminMeResponse {
  isAuthenticated: boolean;
  userId: string;
  userName: string;
  email: string;
  role: string;
  mustChangePassword: boolean;
}

export async function fetchAdminCurrentUser(): Promise<AdminMeResponse> {
  const response = await httpClient.get<AdminMeResponse>('/api/lord/auth/me');
  return response.data;
}

export interface AdminProductListItem {
  id: string;
  title: string;
  slug: string;
  price: number;
  status: ProductStatus;
  categoryName?: string;
  imageCount: number;
  primaryImageUrl?: string;
  isFeatured: boolean;
  featuredSortOrder: number | null;
  createdAt: string;
  updatedAt: string;
}

export type AdminProductSortBy = 'updatedAt' | 'createdAt' | 'price' | 'title';
export type SortDirection = 'asc' | 'desc';

export interface AdminProductQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: ProductStatus;
  categoryId?: string;
  isFeatured?: boolean;
  sortBy?: AdminProductSortBy;
  sortDirection?: SortDirection;
}

export async function fetchAdminProducts(
  params: AdminProductQueryParams = {},
): Promise<PagedResult<AdminProductListItem>> {
  const queryParams: Record<string, string> = {};
  if (params.search) queryParams.search = params.search;
  if (params.status) queryParams.status = params.status;
  if (params.categoryId) queryParams.categoryId = params.categoryId;
  if (typeof params.isFeatured === 'boolean') queryParams.isFeatured = String(params.isFeatured);
  if (params.page) queryParams.page = String(params.page);
  if (params.pageSize) queryParams.pageSize = String(params.pageSize);
  if (params.sortBy) queryParams.sortBy = params.sortBy;
  if (params.sortDirection) queryParams.sortDirection = params.sortDirection;

  const response = await httpClient.get<PagedResult<AdminProductListItem>>('/api/lord/products', {
    params: Object.keys(queryParams).length > 0 ? queryParams : undefined,
  });
  return response.data;
}

export async function createProduct(input: CreateProductInput): Promise<{ id: string }> {
  const response = await httpClient.post<{ id: string }>('/api/lord/products', input);
  return response.data;
}

export interface ProductCategorySelectionResponse {
  productId: string;
  mainCategoryId: string;
  selectedCategoryIds: string[];
}

export interface UpdateProductCategoriesInput {
  mainCategoryId: string;
  selectedCategoryIds: string[];
}

export async function fetchProductCategorySelection(
  productId: string,
): Promise<ProductCategorySelectionResponse> {
  const response = await httpClient.get<ProductCategorySelectionResponse>(
    `/api/lord/products/${productId}/categories`,
    );
  return response.data;
}

export async function updateProductCategories(
  productId: string,
  input: UpdateProductCategoriesInput,
): Promise<ProductCategorySelectionResponse> {
  const response = await httpClient.put<ProductCategorySelectionResponse>(
    `/api/lord/products/${productId}/categories`,
    input,
  );
  return response.data;
}

export async function updateProductStatus(productId: string, status: ProductStatus): Promise<void> {
  await httpClient.put(`/api/lord/products/${productId}/status`, { status });
}

export interface UpdateProductFeaturedInput {
  isFeatured: boolean;
  featuredSortOrder: number | null;
}

export async function updateProductFeatured(
  productId: string,
  input: UpdateProductFeaturedInput,
): Promise<void> {
  await httpClient.put(`/api/lord/products/${productId}/featured`, input);
}

export async function fetchAdminCustomers(
  params: AdminCustomerQueryParams,
): Promise<PagedResult<CustomerListItem>> {
  const response = await httpClient.get<PagedResult<CustomerListItem>>('/api/lord/customers', {
    params,
  });
  return response.data;
}

export async function fetchAdminCustomerDetail(customerId: string): Promise<CustomerDetail> {
  const response = await httpClient.get<CustomerDetail>(`/api/lord/customers/${customerId}`);
  return response.data;
}

export async function fetchAdminCustomerInquiries(
  customerId: string,
  params: CustomerInquiryQueryParams,
): Promise<PagedResult<CustomerInquiryItem>> {
  const response = await httpClient.get<PagedResult<CustomerInquiryItem>>(
    `/api/lord/customers/${customerId}/inquiries`,
    { params },
  );
  return response.data;
}

export async function updateAdminCustomer(
  customerId: string,
  input: UpdateCustomerInput,
): Promise<void> {
  await httpClient.patch(`/api/lord/customers/${customerId}`, input);
}

export async function createAdminCustomer(
  input: CreateCustomerInput,
): Promise<CreateCustomerResult> {
  const response = await httpClient.post<{ id: string }>('/api/lord/customers', input);
  return { id: response.data.id };
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
    `/api/lord/products/${productId}/images/presigned-url`,
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
  await httpClient.post(`/api/lord/products/${productId}/images`, input);
}

export async function deleteProductImage(productId: string, imageId: string): Promise<void> {
  await httpClient.delete(`/api/lord/products/${productId}/images/${imageId}`);
}

export async function removeBackgroundPreview(file: File): Promise<Blob> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await httpClient.post<Blob>(
    '/api/lord/images/remove-background-preview',
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

// --- Product Sales ---

/** Current active (Completed) sale for a product, or null if the product is not sold. */
export async function fetchCurrentProductSale(productId: string): Promise<ProductSaleDto | null> {
  try {
    const response = await httpClient.get<ProductSaleDto>(`/api/lord/products/${productId}/sale`);
    return response.data;
  } catch (err) {
    if (err && typeof err === 'object' && 'response' in err) {
      const axiosErr = err as { response?: { status?: number } };
      if (axiosErr.response?.status === 404) return null;
    }
    throw err;
  }
}

/** Full sale history (including cancelled rows), most recent first. */
export async function fetchProductSaleHistory(productId: string): Promise<ProductSaleDto[]> {
  const response = await httpClient.get<ProductSaleDto[]>(`/api/lord/products/${productId}/sales`);
  return response.data;
}

/** Mark a product as sold — always creates a brand new sale record. */
export async function markProductSold(
  productId: string,
  input: MarkProductSoldInput,
): Promise<ProductSaleDto> {
  const response = await httpClient.post<ProductSaleDto>(
    `/api/lord/products/${productId}/mark-sold`,
    input,
  );
  return response.data;
}

/** Revert a sold product back to Available — cancels the current sale with a reason. */
export async function revertProductSale(
  productId: string,
  input: RevertProductSaleInput,
): Promise<void> {
  await httpClient.post(`/api/lord/products/${productId}/revert-sale`, input);
}

export async function fetchCustomerSales(customerId: string): Promise<CustomerSaleItem[]> {
  const response = await httpClient.get<CustomerSaleItem[]>(
    `/api/lord/customers/${customerId}/sales`,
  );
  return response.data;
}
