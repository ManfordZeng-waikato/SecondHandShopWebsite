import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { ProductQueryParams, ProductSortOption } from '../../../entities/product/types';

const DEFAULT_PAGE_SIZE = 24;
const VALID_SORTS = new Set<string>(['newest', 'price_asc', 'price_desc']);

function parseIntParam(value: string | null, fallback: number): number {
  if (!value) return fallback;
  const n = parseInt(value, 10);
  return Number.isFinite(n) && n > 0 ? n : fallback;
}

function parseFloatParam(value: string | null): number | undefined {
  if (!value) return undefined;
  const n = parseFloat(value);
  return Number.isFinite(n) && n >= 0 ? n : undefined;
}

function parseSortParam(value: string | null): ProductSortOption | undefined {
  return value && VALID_SORTS.has(value) ? (value as ProductSortOption) : undefined;
}

export function useProductFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const params: ProductQueryParams = useMemo(
    () => ({
      page: parseIntParam(searchParams.get('page'), 1),
      pageSize: parseIntParam(searchParams.get('pageSize'), DEFAULT_PAGE_SIZE),
      category: searchParams.get('category') || undefined,
      minPrice: parseFloatParam(searchParams.get('minPrice')),
      maxPrice: parseFloatParam(searchParams.get('maxPrice')),
      status: searchParams.get('status') || undefined,
      sort: parseSortParam(searchParams.get('sort')),
    }),
    [searchParams],
  );

  const setFilters = useCallback(
    (updates: Partial<ProductQueryParams>, resetPage = true) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);

          if (resetPage && !('page' in updates)) {
            next.delete('page');
          }

          for (const [key, value] of Object.entries(updates)) {
            if (value === undefined || value === null || value === '') {
              next.delete(key);
            } else {
              next.set(key, String(value));
            }
          }

          if (next.get('page') === '1') next.delete('page');
          if (next.get('pageSize') === String(DEFAULT_PAGE_SIZE))
            next.delete('pageSize');
          if (next.get('sort') === 'newest') next.delete('sort');

          return next;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  const resetFilters = useCallback(() => {
    setSearchParams({}, { replace: true });
  }, [setSearchParams]);

  return { params, setFilters, resetFilters } as const;
}
