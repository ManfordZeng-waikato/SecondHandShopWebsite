import type { Category } from '../../entities/category/types';
import type { CreateInquiryInput } from '../../entities/inquiry/types';
import type { CreateProductInput, Product, ProductStatus } from '../../entities/product/types';
import { mockCategories, mockProducts } from './data';

let productsStore: Product[] = [...mockProducts];
const categoriesStore: Category[] = [...mockCategories];
const inquiryStore: CreateInquiryInput[] = [];

function wait(delay = 250): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, delay));
}

export async function getMockCategories(): Promise<Category[]> {
  await wait();
  return categoriesStore;
}

export async function getMockProducts(categoryId?: string): Promise<Product[]> {
  await wait();
  return categoryId
    ? productsStore.filter((item) => item.categoryId === categoryId)
    : productsStore;
}

export async function getMockProductBySlug(slug: string): Promise<Product | null> {
  await wait();
  return productsStore.find((item) => item.slug === slug) ?? null;
}

export async function createMockInquiry(input: CreateInquiryInput): Promise<{ id: string }> {
  await wait();
  inquiryStore.push(input);
  return { id: crypto.randomUUID() };
}

export async function createMockProduct(input: CreateProductInput): Promise<{ id: string }> {
  await wait();
  const matchedCategory = categoriesStore.find((item) => item.id === input.categoryId);
  const createdProduct: Product = {
    id: crypto.randomUUID(),
    title: input.title,
    slug: input.slug,
    description: input.description,
    price: input.price,
    condition: input.condition,
    status: 'Available',
    categoryId: input.categoryId,
    categoryName: matchedCategory?.name,
    images: [],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  productsStore = [createdProduct, ...productsStore];
  return { id: createdProduct.id };
}

export async function getMockProductsForAdmin(status?: ProductStatus) {
  await wait();
  const filtered = status
    ? productsStore.filter((item) => item.status === status)
    : productsStore;

  return filtered.map((item) => ({
    id: item.id,
    title: item.title,
    slug: item.slug,
    price: item.price,
    condition: item.condition,
    status: item.status,
    categoryName: item.categoryName,
    imageCount: item.images.length,
    primaryImageUrl: item.images.find((img) => img.isPrimary)?.displayUrl ?? item.images[0]?.displayUrl,
    createdAt: item.createdAt,
    updatedAt: item.updatedAt,
  }));
}

export async function updateMockProductStatus(
  productId: string,
  status: ProductStatus,
): Promise<void> {
  await wait();
  productsStore = productsStore.map((item) =>
    item.id === productId
      ? {
          ...item,
          status,
          updatedAt: new Date().toISOString(),
        }
      : item,
  );
}
