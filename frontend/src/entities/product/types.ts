export type ProductStatus = 'Available' | 'Sold' | 'OffShelf';
export type ProductSortOption = 'newest' | 'price_asc' | 'price_desc';

export interface ProductImage {
  id: string;
  objectKey: string;
  displayUrl: string;
  altText?: string;
  sortOrder: number;
  isPrimary: boolean;
}

export interface Product {
  id: string;
  title: string;
  slug: string;
  description: string;
  price: number;
  status: ProductStatus;
  categoryId: string;
  categoryName?: string;
  images: ProductImage[];
  createdAt: string;
  updatedAt: string;
}

export interface ProductListItem {
  id: string;
  title: string;
  slug: string;
  price: number;
  coverImageUrl: string | null;
  categoryName: string | null;
  status: ProductStatus;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ProductQueryParams {
  page?: number;
  pageSize?: number;
  category?: string;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  status?: string;
  sort?: ProductSortOption;
}

export interface CreateProductInput {
  title: string;
  slug: string;
  description: string;
  price: number;
  categoryId: string;
}
