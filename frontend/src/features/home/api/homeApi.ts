import type { ProductListItem } from '../../../entities/product/types';
import { env } from '../../../shared/config/env';
import { fetchProductsPaged } from '../../catalog/api/catalogApi';

const MOCK_FEATURED: ProductListItem[] = [
  {
    id: 'feat-1',
    title: 'Vintage Oak Dining Table',
    slug: 'vintage-oak-dining-table',
    price: 280,
    coverImageUrl: 'https://picsum.photos/seed/oak-table/400/300',
    categoryName: 'Furniture',
    status: 'Available',
    condition: 'Good',
    createdAt: '2026-03-20T10:00:00Z',
  },
  {
    id: 'feat-2',
    title: 'Mid-Century Lounge Chair',
    slug: 'mid-century-lounge-chair',
    price: 165,
    coverImageUrl: 'https://picsum.photos/seed/lounge-chair/400/300',
    categoryName: 'Furniture',
    status: 'Available',
    condition: 'LikeNew',
    createdAt: '2026-03-18T14:30:00Z',
  },
  {
    id: 'feat-3',
    title: 'Retro Wooden Bookshelf',
    slug: 'retro-wooden-bookshelf',
    price: 120,
    coverImageUrl: 'https://picsum.photos/seed/bookshelf/400/300',
    categoryName: 'Furniture',
    status: 'Available',
    condition: 'Good',
    createdAt: '2026-03-15T09:00:00Z',
  },
  {
    id: 'feat-4',
    title: 'Ceramic Bedside Lamp',
    slug: 'ceramic-bedside-lamp',
    price: 45,
    coverImageUrl: 'https://picsum.photos/seed/table-lamp/400/300',
    categoryName: 'Home Appliances',
    status: 'Sold',
    condition: 'LikeNew',
    createdAt: '2026-03-12T11:00:00Z',
  },
  {
    id: 'feat-5',
    title: 'Solid Pine Coffee Table',
    slug: 'solid-pine-coffee-table',
    price: 195,
    coverImageUrl: 'https://picsum.photos/seed/pine-table/400/300',
    categoryName: 'Furniture',
    status: 'Available',
    condition: 'Fair',
    createdAt: '2026-03-10T16:00:00Z',
  },
];

export async function fetchFeaturedProducts(): Promise<ProductListItem[]> {
  if (env.useMockApi) {
    await new Promise((r) => setTimeout(r, 300));
    return MOCK_FEATURED;
  }

  const result = await fetchProductsPaged({ pageSize: 5, sort: 'newest' });
  return result.items;
}
