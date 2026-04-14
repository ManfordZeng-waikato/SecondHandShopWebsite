import { useQuery } from '@tanstack/react-query';
import { fetchAnalyticsOverview } from './api';
import type { AnalyticsRangeKey } from './types';

export function useAnalyticsOverview(range: AnalyticsRangeKey) {
  return useQuery({
    queryKey: ['admin', 'analytics', 'overview', range],
    queryFn: () => fetchAnalyticsOverview(range),
    staleTime: 60_000,
  });
}
