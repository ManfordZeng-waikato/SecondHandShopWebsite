import type { Category } from '../../entities/category/types';
import type { Product } from '../../entities/product/types';

export const mockCategories: Category[] = [
  {
    id: 'cat-electronics',
    name: 'Electronics',
    slug: 'electronics',
    sortOrder: 1,
    isActive: true,
  },
  {
    id: 'cat-furniture',
    name: 'Furniture',
    slug: 'furniture',
    sortOrder: 2,
    isActive: true,
  },
  {
    id: 'cat-appliances',
    name: 'Home Appliances',
    slug: 'home-appliances',
    sortOrder: 3,
    isActive: true,
  },
];

export const mockProducts: Product[] = [];
