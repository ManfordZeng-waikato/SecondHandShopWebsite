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
    images: [
      {
        id: crypto.randomUUID(),
        url: 'https://picsum.photos/seed/new-product/800/500',
        altText: `${input.title} preview`,
        sortOrder: 1,
        isPrimary: true,
      },
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  productsStore = [createdProduct, ...productsStore];
  return { id: createdProduct.id };
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
