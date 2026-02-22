export type ProductStatus = 'Available' | 'Sold' | 'OffShelf';
export type ProductCondition = 'LikeNew' | 'Good' | 'Fair' | 'NeedsRepair';

export interface ProductImage {
  id: string;
  url: string;
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
  condition: ProductCondition;
  status: ProductStatus;
  categoryId: string;
  categoryName?: string;
  images: ProductImage[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductInput {
  title: string;
  slug: string;
  description: string;
  price: number;
  condition: ProductCondition;
  categoryId: string;
}
