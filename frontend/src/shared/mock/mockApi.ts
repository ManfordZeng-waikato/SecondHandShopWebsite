import type { Category } from '../../entities/category/types';
import type { CreateInquiryInput } from '../../entities/inquiry/types';
import type {
  CreateProductInput,
  Product,
  ProductListItem,
  ProductStatus,
} from '../../entities/product/types';
import { mockCategories, mockProducts } from './data';

let productsStore: Product[] = [...mockProducts];
const categoriesStore: Category[] = [...mockCategories];
const inquiryStore: CreateInquiryInput[] = [];
const FEATURED_SORT_ORDER_MIN = 0;
const FEATURED_SORT_ORDER_MAX = 999;
let featuredStateStore: Record<string, { isFeatured: boolean; featuredSortOrder: number | null }> =
  Object.fromEntries(productsStore.map((product) => [product.id, { isFeatured: false, featuredSortOrder: null }]));

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
    status: 'Available',
    categoryId: input.categoryId,
    categoryName: matchedCategory?.name,
    images: [],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  productsStore = [createdProduct, ...productsStore];
  featuredStateStore[createdProduct.id] = { isFeatured: false, featuredSortOrder: null };
  return { id: createdProduct.id };
}

function getFeaturedState(productId: string) {
  return featuredStateStore[productId] ?? { isFeatured: false, featuredSortOrder: null };
}

export async function getMockProductsForAdmin(status?: ProductStatus, isFeatured?: boolean) {
  await wait();
  let filtered = status
    ? productsStore.filter((item) => item.status === status)
    : productsStore;

  if (typeof isFeatured === 'boolean') {
    filtered = filtered.filter((item) => getFeaturedState(item.id).isFeatured === isFeatured);
  }

  return filtered.map((item) => ({
    ...getFeaturedState(item.id),
    id: item.id,
    title: item.title,
    slug: item.slug,
    price: item.price,
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
  const shouldClearFeatured = status !== 'Available';

  productsStore = productsStore.map((item) =>
    item.id === productId
      ? {
          ...item,
          status,
          updatedAt: new Date().toISOString(),
        }
      : item,
  );

  if (shouldClearFeatured) {
    featuredStateStore[productId] = {
      isFeatured: false,
      featuredSortOrder: null,
    };
  }
}

export async function updateMockProductFeatured(
  productId: string,
  isFeatured: boolean,
  featuredSortOrder: number | null,
): Promise<void> {
  await wait();

  const product = productsStore.find((item) => item.id === productId);
  if (!product) {
    throw new Error('Product was not found.');
  }

  if (isFeatured && product.status !== 'Available') {
    throw new Error('Only available products can be featured.');
  }

  const normalizedSortOrder = isFeatured ? featuredSortOrder : null;
  if (
    normalizedSortOrder !== null &&
    (normalizedSortOrder < FEATURED_SORT_ORDER_MIN || normalizedSortOrder > FEATURED_SORT_ORDER_MAX)
  ) {
    throw new Error(
      `Featured sort order must be between ${FEATURED_SORT_ORDER_MIN} and ${FEATURED_SORT_ORDER_MAX}. ` +
      'Smaller values appear earlier.',
    );
  }

  featuredStateStore[productId] = {
    isFeatured,
    featuredSortOrder: normalizedSortOrder,
  };

  productsStore = productsStore.map((item) =>
    item.id === productId
      ? {
          ...item,
          updatedAt: new Date().toISOString(),
        }
      : item,
  );
}

export async function getMockFeaturedProducts(limit = 8): Promise<ProductListItem[]> {
  await wait();

  const safeLimit = Math.max(1, limit);
  const featuredProducts = productsStore
    .filter((item) => item.status === 'Available')
    .filter((item) => getFeaturedState(item.id).isFeatured)
    .sort((left, right) => {
      const leftState = getFeaturedState(left.id);
      const rightState = getFeaturedState(right.id);

      const leftHasSortOrder = leftState.featuredSortOrder !== null;
      const rightHasSortOrder = rightState.featuredSortOrder !== null;
      if (leftHasSortOrder !== rightHasSortOrder) {
        return leftHasSortOrder ? -1 : 1;
      }

      if (leftState.featuredSortOrder !== rightState.featuredSortOrder) {
        return (leftState.featuredSortOrder ?? 0) - (rightState.featuredSortOrder ?? 0);
      }

      const createdAtCompare =
        new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
      if (createdAtCompare !== 0) {
        return createdAtCompare;
      }

      return left.id.localeCompare(right.id);
    })
    .slice(0, safeLimit);

  return featuredProducts.map((item) => ({
    id: item.id,
    title: item.title,
    slug: item.slug,
    price: item.price,
    coverImageUrl: item.images.find((img) => img.isPrimary)?.displayUrl ?? item.images[0]?.displayUrl ?? null,
    categoryName: item.categoryName ?? null,
    status: item.status,
    createdAt: item.createdAt,
  }));
}
