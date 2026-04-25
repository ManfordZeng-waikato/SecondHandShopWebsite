import { AxiosError } from 'axios';
import {
  createAdminCustomer,
  createProductImageUploadUrl,
  fetchAdminCustomerDetail,
  fetchAdminCustomerInquiries,
  fetchAdminProducts,
  fetchCurrentProductSale,
  markProductSold,
  revertProductSale,
  updateAdminCustomer,
  updateProductFeatured,
  updateProductStatus,
  uploadBlobToR2,
  uploadImageToR2,
} from '../adminApi';
import { httpClient } from '../../../../shared/api/httpClient';

vi.mock('../../../../shared/api/httpClient', () => ({
  httpClient: {
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

  it('maps customer API wrappers to their admin endpoints', async () => {
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { id: 'customer-1' } });
    vi.mocked(httpClient.get).mockResolvedValueOnce({ data: { items: [], totalCount: 0 } });
    vi.mocked(httpClient.post).mockResolvedValueOnce({ data: { id: 'customer-2' } });
    vi.mocked(httpClient.patch).mockResolvedValueOnce({ data: undefined });

    await expect(fetchAdminCustomerDetail('customer-1')).resolves.toEqual({ id: 'customer-1' });
    await expect(fetchAdminCustomerInquiries('customer-1', { page: 2 })).resolves.toEqual({
      items: [],
      totalCount: 0,
    });
    await expect(createAdminCustomer({ name: 'Alice' })).resolves.toEqual({ id: 'customer-2' });
    await updateAdminCustomer('customer-1', { notes: 'Updated' });

    expect(httpClient.get).toHaveBeenNthCalledWith(1, '/api/lord/customers/customer-1');
    expect(httpClient.get).toHaveBeenNthCalledWith(2, '/api/lord/customers/customer-1/inquiries', {
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
});
