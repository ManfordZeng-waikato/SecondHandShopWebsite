import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AdminNewProductPage } from '../AdminNewProductPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import {
  addProductImage,
  createProduct,
  createProductImageUploadUrl,
  removeBackgroundPreview,
  updateProductCategories,
  uploadBlobToR2,
  uploadImageToR2,
} from '../../features/admin/api/adminApi';
import { fetchCategoryTree } from '../../features/catalog/api/catalogApi';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock('../../features/admin/api/adminApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/admin/api/adminApi')>(
    '../../features/admin/api/adminApi',
  );
  return {
    ...actual,
    addProductImage: vi.fn(),
    createProduct: vi.fn(),
    createProductImageUploadUrl: vi.fn(),
    removeBackgroundPreview: vi.fn(),
    updateProductCategories: vi.fn(),
    uploadBlobToR2: vi.fn(),
    uploadImageToR2: vi.fn(),
  };
});

vi.mock('../../features/catalog/api/catalogApi', async () => {
  const actual = await vi.importActual<typeof import('../../features/catalog/api/catalogApi')>(
    '../../features/catalog/api/catalogApi',
  );
  return {
    ...actual,
    fetchCategoryTree: vi.fn(),
  };
});

function imageFile(name = 'chair.jpg') {
  return new File(['image-bytes'], name, { type: 'image/jpeg' });
}

async function fillRequiredProductForm(file = imageFile()) {
  await screen.findByText('Create new product');
  await userEvent.type(screen.getByLabelText(/title/i), 'Vintage Chair');
  await userEvent.type(screen.getByLabelText(/description/i), 'A comfortable vintage chair.');
  await userEvent.type(screen.getByLabelText(/price/i), '125');
  await userEvent.click(screen.getByRole('button', { name: 'Chairs' }));
  await userEvent.upload(screen.getByLabelText(/select product images/i), file);
  await screen.findByText('1 image(s) selected');
  return file;
}

describe('AdminNewProductPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    URL.createObjectURL = vi.fn(() => 'blob:preview');
    URL.revokeObjectURL = vi.fn();
    vi.mocked(fetchCategoryTree).mockResolvedValue([
      {
        id: 'root-1',
        name: 'Furniture',
        slug: 'furniture',
        children: [
          {
            id: 'category-1',
            name: 'Chairs',
            slug: 'chairs',
            children: [],
          },
        ],
      },
    ]);
    vi.mocked(createProduct).mockResolvedValue({ id: 'product-1' });
    vi.mocked(updateProductCategories).mockResolvedValue({
      productId: 'product-1',
      mainCategoryId: 'category-1',
      selectedCategoryIds: ['category-1'],
    });
    vi.mocked(createProductImageUploadUrl).mockResolvedValue({
      objectKey: 'products/product-1/chair.jpg',
      putUrl: 'https://r2.example.test/upload',
      expiresInSeconds: 600,
    });
    vi.mocked(uploadImageToR2).mockResolvedValue(undefined);
    vi.mocked(uploadBlobToR2).mockResolvedValue(undefined);
    vi.mocked(addProductImage).mockResolvedValue(undefined);
  });

  it('shows validation errors when required fields are missing', async () => {
    renderWithProviders(<AdminNewProductPage />);

    await userEvent.click(await screen.findByRole('button', { name: /create product/i }));

    expect(await screen.findByText(/title, slug and description are required/i)).toBeInTheDocument();
    expect(createProduct).not.toHaveBeenCalled();
  });

  it('shows a partial image upload error when presigned URL creation fails', async () => {
    vi.mocked(createProductImageUploadUrl).mockRejectedValue(new Error('presign failed'));
    renderWithProviders(<AdminNewProductPage />);
    await fillRequiredProductForm();

    await userEvent.click(screen.getByRole('button', { name: /create product/i }));

    expect(await screen.findByText(/image upload stopped after 0\/1 images/i)).toBeInTheDocument();
    expect(addProductImage).not.toHaveBeenCalled();
    expect(mockNavigate).not.toHaveBeenCalled();
  });

  it('allows original image upload after background removal is rejected', async () => {
    vi.mocked(removeBackgroundPreview).mockRejectedValue(new Error('Remove.bg API key invalid.'));
    renderWithProviders(<AdminNewProductPage />);
    const file = await fillRequiredProductForm();

    await userEvent.click(screen.getByRole('button', { name: /remove background/i }));
    expect(await screen.findByText(/remove\.bg api key invalid/i)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: /create product/i }));

    await waitFor(() => {
      expect(uploadImageToR2).toHaveBeenCalledWith('https://r2.example.test/upload', file);
    });
    expect(uploadBlobToR2).not.toHaveBeenCalled();
    expect(addProductImage).toHaveBeenCalledWith('product-1', {
      objectKey: 'products/product-1/chair.jpg',
      altText: 'Vintage Chair',
      sortOrder: 0,
      isPrimary: true,
    });
  });

  it('uploads the selected cutout image and registers it with product image metadata', async () => {
    const cutout = new Blob(['cutout'], { type: 'image/png' });
    vi.mocked(removeBackgroundPreview).mockResolvedValue(cutout);
    renderWithProviders(<AdminNewProductPage />);
    await fillRequiredProductForm();

    await userEvent.click(screen.getByRole('button', { name: /remove background/i }));
    expect(await screen.findByText(/background removed/i)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: /create product/i }));

    await waitFor(() => {
      expect(createProductImageUploadUrl).toHaveBeenCalledWith(
        'product-1',
        'chair-nobg.png',
        'image/png',
      );
    });
    expect(uploadBlobToR2).toHaveBeenCalledWith('https://r2.example.test/upload', cutout, 'image/png');
    expect(addProductImage).toHaveBeenCalledWith('product-1', {
      objectKey: 'products/product-1/chair.jpg',
      altText: 'Vintage Chair',
      sortOrder: 0,
      isPrimary: true,
    });
    expect(mockNavigate).toHaveBeenCalledWith('/lord/products', {
      state: expect.objectContaining({ forceRefreshProducts: true }),
    });
  });
});
