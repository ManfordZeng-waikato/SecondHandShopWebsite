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

export const mockProducts: Product[] = [
  {
    id: 'prod-iphone-13',
    title: 'iPhone 13 128GB',
    slug: 'iphone-13-128gb',
    description: 'Well maintained iPhone 13, battery health 88%, no major scratches.',
    price: 420,
    condition: 'Good',
    status: 'Available',
    categoryId: 'cat-electronics',
    categoryName: 'Electronics',
    images: [
      {
        id: 'img-iphone-1',
        objectKey: 'products/mock/iphone13.jpg',
        displayUrl: 'https://picsum.photos/seed/iphone13/800/500',
        altText: 'iPhone 13 front view',
        sortOrder: 1,
        isPrimary: true,
      },
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'prod-office-chair',
    title: 'Ergonomic Office Chair',
    slug: 'ergonomic-office-chair',
    description: 'Mesh ergonomic chair with adjustable armrest and lumbar support.',
    price: 95,
    condition: 'Fair',
    status: 'Sold',
    categoryId: 'cat-furniture',
    categoryName: 'Furniture',
    images: [
      {
        id: 'img-chair-1',
        objectKey: 'products/mock/chair01.jpg',
        displayUrl: 'https://picsum.photos/seed/chair01/800/500',
        altText: 'Office chair side view',
        sortOrder: 1,
        isPrimary: true,
      },
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'prod-microwave',
    title: 'Panasonic Microwave Oven',
    slug: 'panasonic-microwave-oven',
    description: 'Compact microwave, fully working, suitable for dorm or small kitchen.',
    price: 70,
    condition: 'LikeNew',
    status: 'Available',
    categoryId: 'cat-appliances',
    categoryName: 'Home Appliances',
    images: [
      {
        id: 'img-micro-1',
        objectKey: 'products/mock/micro001.jpg',
        displayUrl: 'https://picsum.photos/seed/micro001/800/500',
        altText: 'Microwave on table',
        sortOrder: 1,
        isPrimary: true,
      },
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];
