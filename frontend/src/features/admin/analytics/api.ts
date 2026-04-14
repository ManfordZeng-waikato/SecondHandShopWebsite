import { httpClient } from '../../../shared/api/httpClient';
import type { AnalyticsOverview, AnalyticsRangeKey } from './types';

export async function fetchAnalyticsOverview(
  range: AnalyticsRangeKey,
): Promise<AnalyticsOverview> {
  const response = await httpClient.get<AnalyticsOverview>('/api/lord/analytics/overview', {
    params: { range },
  });
  return response.data;
}
