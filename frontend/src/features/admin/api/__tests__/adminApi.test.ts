import { AxiosError } from 'axios';
import {
  addProductImage,
  changeAdminInitialPassword,
  createProduct,
  createAdminCustomer,
  deleteProductImage,
  createProductImageUploadUrl,
  fetchAdminCurrentUser,
  fetchAdminCustomerDetail,
  fetchAdminCustomerInquiries,
  fetchAdminCustomers,
  fetchAdminProducts,
  fetchCustomerSales,
  fetchCurrentProductSale,
  fetchProductCategorySelection,
  fetchProductInquiries,
  fetchProductSaleHistory,
  loginAdmin,
  logoutAdmin,
  markProductSold,
  removeBackgroundPreview,
  revertProductSale,
  updateProductCategories,
  updateAdminCustomer,
  updateProductFeatured,
  updateProductStatus,
  uploadBlobToR2,
  uploadImageToR2,
} from '../adminApi';
import { httpClient } from '../../../../shared/api/httpClient';

vi.mock('../../../../shared/api/httpClient', () => ({
  httpClient: {
    delete: vi.fn(),
    get: vi.fn(),
    patch: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe('adminApi', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  it('maps auth API wrappers to lord auth endpoints', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({
      data: { expiresAt: '2026-04-25T00:00:00Z', requiresPasswordChange: false },
    });
    vi.mocked(httpClient.post).mockResolvedValueOnce({
      data: { success: true, requiresReLogin: true, message: 'Changed' },
    });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: undefined });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { userName: 'lord' } });

    await expect(loginAdmin('lord', 'secret')).resolves.toMatchObject({ requiresPasswordChange: false });
    await expect(
      changeAdminInitialPassword({
        currentPassword: 'old',
        newPassword: 'new-secret',
        confirmNewPassword: 'new-secret',
      }),
    ).resolves.toMatchObject({ success: true });
    await logoutAdmin();
    await expect(fetchAdminCurrentUser()).resolves.toEqual({ userName: 'lord' });

    expect(httpClient.post).toHaveBeenNthCalledWith(1, '/api/lord/auth/login', {
      userName: 'lord',
      password: 'secret',
    });
    expect(httpClient.post).toHaveBeenNthCalledWith(2, '/api/lord/auth/change-initial-password', {
      currentPassword: 'old',
      newPassword: 'new-secret',
      confirmNewPassword: 'new-secret',
    });
    expect(httpClient.post).toHaveBeenNthCalledWith(3, '/api/lord/auth/logout');
    expect(httpClient.get).toHaveBeenCalledWith('/api/lord/auth/me');
  });

  it('fetches admin products without params when no filters are supplied', async () => {
    const result = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
      isFallback: false,
    };
    vi.mocked(httpClient.get).mockResolvedValue({ data: result });

    await expect(fetchAdminProducts()).resolves.toBe(result);

    expect(httpClient.get).toHaveBeenCalledWith('/api/lord/products', {
      params: undefined,
    });
  });

  it('creates products and updates category selection', async () => {
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { id: 'product-1' } });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { productId: 'product-1' } });
    vi.mocked(httpClient.put).mockResolvedValueOnce({ data: { productId: 'product-1', selectedCategoryIds: ['cat-1'] } });

    await expect(
      createProduct({
        title: 'Oak Chair',
        slug: 'oak-chair',
        description: 'Solid oak chair',
        price: 120,
        categoryId: 'cat-1',
      }),
    ).resolves.toEqual({ id: 'product-1' });
    await expect(fetchProductCategorySelection('product-1')).resolves.toEqual({ productId: 'product-1' });
    await expect(
      updateProductCategories('product-1', {
        mainCategoryId: 'cat-1',
        selectedCategoryIds: ['cat-1'],
      }),
    ).resolves.toEqual({ productId: 'product-1', selectedCategoryIds: ['cat-1'] });

    expect(httpClient.post).toHaveBeenCalledWith('/api/lord/products', {
      title: 'Oak Chair',
      slug: 'oak-chair',
      description: 'Solid oak chair',
      price: 120,
      categoryId: 'cat-1',
    });
    expect(httpClient.get).toHaveBeenCalledWith('/api/lord/products/product-1/categories');
    expect(httpClient.put).toHaveBeenCalledWith('/api/lord/products/product-1/categories', {
      mainCategoryId: 'cat-1',
      selectedCategoryIds: ['cat-1'],
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('serializes admin product filters before fetching products', async () => {
    const result = {
      items: [],
      page: 2,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: true,
      isFallback: false,
    };
    vi.mocked(httpClient.get).mockResolvedValue({ data: result });

    await expect(
      fetchAdminProducts({
        search: 'oak',
        status: 'Available',
        categoryId: 'category-1',
        isFeatured: false,
        page: 2,
        pageSize: 10,
        sortBy: 'price',
        sortDirection: 'asc',
      }),
    ).resolves.toBe(result);

    expect(httpClient.get).toHaveBeenCalledWith('/api/lord/products', {
      params: {
        search: 'oak',
        status: 'Available',
        categoryId: 'category-1',
        isFeatured: 'false',
        page: '2',
        pageSize: '10',
        sortBy: 'price',
        sortDirection: 'asc',
      },
    });
  });

  it('posts image upload metadata when requesting a presigned URL', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: {
        objectKey: 'products/product-1/chair.jpg',
        putUrl: 'https://r2.example.test/upload',
        expiresInSeconds: 600,
      },
    });

    await expect(
      createProductImageUploadUrl('product-1', 'chair.jpg', 'image/jpeg'),
    ).resolves.toMatchObject({ objectKey: 'products/product-1/chair.jpg' });

    expect(httpClient.post).toHaveBeenCalledWith(
      '/api/lord/products/product-1/images/presigned-url',
      { fileName: 'chair.jpg', contentType: 'image/jpeg' },
    );
  });

  it('uploads original files to R2 with the file content type', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 200 }));
    const file = new File(['image'], 'chair.jpg', { type: 'image/jpeg' });

    await uploadImageToR2('https://r2.example.test/upload', file);

    expect(fetch).toHaveBeenCalledWith('https://r2.example.test/upload', {
      method: 'PUT',
      headers: { 'Content-Type': 'image/jpeg' },
      body: file,
    });
  });

  it('uploads cutout blobs to R2 with the selected content type', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 200 }));
    const blob = new Blob(['cutout'], { type: 'image/png' });

    await uploadBlobToR2('https://r2.example.test/upload', blob, 'image/png');

    expect(fetch).toHaveBeenCalledWith('https://r2.example.test/upload', {
      method: 'PUT',
      headers: { 'Content-Type': 'image/png' },
      body: blob,
    });
  });

  it('returns null when the current product sale endpoint returns 404', async () => {
    vi.mocked(httpClient.get).mockRejectedValue(
      new AxiosError('Not Found', 'ERR_BAD_REQUEST', undefined, undefined, {
        status: 404,
        statusText: 'Not Found',
        headers: {},
        config: {} as never,
        data: {},
      }),
    );

    await expect(fetchCurrentProductSale('product-1')).resolves.toBeNull();
  });

  it('returns current product sale when the endpoint succeeds', async () => {
    vi.mocked(httpClient.get).mockResolvedValue({ data: { id: 'sale-1' } });

    await expect(fetchCurrentProductSale('product-1')).resolves.toEqual({ id: 'sale-1' });

    expect(httpClient.get).toHaveBeenCalledWith('/api/lord/products/product-1/sale');
  });

  it('maps customer API wrappers to their admin endpoints', async () => {
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { items: [], totalCount: 0 } });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { id: 'customer-1' } });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { items: [], totalCount: 0 } });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { id: 'customer-2' } });
    vi.mocked(httpClient.patch).mockResolvedValueOnce({ data: undefined });

    await expect(fetchAdminCustomers({ search: 'alice', page: 1 })).resolves.toEqual({
      items: [],
      totalCount: 0,
    });
    await expect(fetchAdminCustomerDetail('customer-1')).resolves.toEqual({ id: 'customer-1' });
    await expect(fetchAdminCustomerInquiries('customer-1', { page: 2 })).resolves.toEqual({
      items: [],
      totalCount: 0,
    });
    await expect(createAdminCustomer({ name: 'Alice' })).resolves.toEqual({ id: 'customer-2' });
    await updateAdminCustomer('customer-1', { notes: 'Updated' });

    expect(httpClient.get).toHaveBeenNthCalledWith(1, '/api/lord/customers', {
      params: { search: 'alice', page: 1 },
    });
    expect(httpClient.get).toHaveBeenNthCalledWith(2, '/api/lord/customers/customer-1');
    expect(httpClient.get).toHaveBeenNthCalledWith(3, '/api/lord/customers/customer-1/inquiries', {
      params: { page: 2 },
    });
    expect(httpClient.post).toHaveBeenCalledWith('/api/lord/customers', { name: 'Alice' });
    expect(httpClient.patch).toHaveBeenCalledWith('/api/lord/customers/customer-1', { notes: 'Updated' });
  });

  it('maps product status and sale commands to their admin endpoints', async () => {
    const soldInput = {
      finalSoldPrice: 125,
      soldAtUtc: '2026-04-25T00:00:00Z',
      paymentMethod: 'Cash' as const,
    };
    vi.mocked(httpClient.put).mockResolvedValue({ data: undefined });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { id: 'sale-1' } });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: undefined });

    await updateProductStatus('product-1', 'Sold');
    await updateProductFeatured('product-1', { isFeatured: true, featuredSortOrder: 3 });
    await expect(markProductSold('product-1', soldInput)).resolves.toEqual({ id: 'sale-1' });
    await revertProductSale('product-1', { reason: 'BuyerBackedOut', cancellationNote: 'Cancelled' });

    expect(httpClient.put).toHaveBeenNthCalledWith(1, '/api/lord/products/product-1/status', {
      status: 'Sold',
    });
    expect(httpClient.put).toHaveBeenNthCalledWith(2, '/api/lord/products/product-1/featured', {
      isFeatured: true,
      featuredSortOrder: 3,
    });
    expect(httpClient.post).toHaveBeenNthCalledWith(
      1,
      '/api/lord/products/product-1/mark-sold',
      soldInput,
    );
    expect(httpClient.post).toHaveBeenNthCalledWith(2, '/api/lord/products/product-1/revert-sale', {
      reason: 'BuyerBackedOut',
      cancellationNote: 'Cancelled',
    });
  });

  it('maps product image and background removal wrappers', async () => {
    const file = new File(['image'], 'chair.png', { type: 'image/png' });
    const blob = new Blob(['cutout'], { type: 'image/png' });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: undefined });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: blob });
    vi.mocked(httpClient.delete).mockResolvedValueOnce({ data: undefined });

    await addProductImage('product-1', {
      objectKey: 'products/product-1/chair.png',
      sortOrder: 1,
      isPrimary: true,
    });
    await expect(removeBackgroundPreview(file)).resolves.toBe(blob);
    await deleteProductImage('product-1', 'image-1');

    expect(httpClient.post).toHaveBeenNthCalledWith(1, '/api/lord/products/product-1/images', {
      objectKey: 'products/product-1/chair.png',
      sortOrder: 1,
      isPrimary: true,
    });
    expect(httpClient.post).toHaveBeenNthCalledWith(
      2,
      '/api/lord/images/remove-background-preview',
      expect.any(FormData),
      {
        responseType: 'blob',
        timeout: 60000,
        headers: { 'Content-Type': 'multipart/form-data' },
      },
    );
    expect(httpClient.delete).toHaveBeenCalledWith('/api/lord/products/product-1/images/image-1');
  });

  it('retries R2 uploads after transient failures and rejects client errors immediately', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(new Response(null, { status: 503 }))
      .mockResolvedValueOnce(new Response(null, { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 403 }));
    const file = new File(['image'], 'chair.jpg', { type: '' });

    await uploadImageToR2('https://r2.example.test/upload', file);
    await expect(uploadBlobToR2('https://r2.example.test/upload', new Blob(['x']), 'image/png')).rejects.toThrow(
      'Upload rejected (403)',
    );

    expect(fetch).toHaveBeenNthCalledWith(1, 'https://r2.example.test/upload', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/octet-stream' },
      body: file,
    });
    expect(fetch).toHaveBeenCalledTimes(3);
  });

  it('throws the last upload error after retries are exhausted', async () => {
    vi.mocked(fetch).mockRejectedValue(new Error('network down'));

    await expect(uploadBlobToR2('https://r2.example.test/upload', new Blob(['x']), 'image/png')).rejects.toThrow(
      'network down',
    );

    expect(fetch).toHaveBeenCalledTimes(3);
  });

  it('throws status error when upload retries are exhausted without an exception', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 503 }));

    await expect(uploadImageToR2('https://r2.example.test/upload', new File(['x'], 'x.bin'))).rejects.toThrow(
      'Upload failed with status 503',
    );

    expect(fetch).toHaveBeenCalledTimes(3);
  });

  it('fetches sale history, product inquiries, and customer sales', async () => {
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [{ id: 'sale-1' }] });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [{ id: 'inquiry-1' }] });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [{ id: 'customer-sale-1' }] });

    await expect(fetchProductSaleHistory('product-1')).resolves.toEqual([{ id: 'sale-1' }]);
    await expect(fetchProductInquiries('product-1')).resolves.toEqual([{ id: 'inquiry-1' }]);
    await expect(fetchCustomerSales('customer-1')).resolves.toEqual([{ id: 'customer-sale-1' }]);

    expect(httpClient.get).toHaveBeenNthCalledWith(1, '/api/lord/products/product-1/sales');
    expect(httpClient.get).toHaveBeenNthCalledWith(2, '/api/lord/products/product-1/inquiries');
    expect(httpClient.get).toHaveBeenNthCalledWith(3, '/api/lord/customers/customer-1/sales');
  });

  it('rethrows non-404 errors when fetching current product sale', async () => {
    vi.mocked(httpClient.get).mockRejectedValue(new Error('server failed'));

    await expect(fetchCurrentProductSale('product-1')).rejects.toThrow('server failed');
  });
});
