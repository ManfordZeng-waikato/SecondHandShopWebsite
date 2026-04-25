import { httpClient } from '../../../../shared/api/httpClient';
import { fetchAnalyticsOverview } from '../api';

vi.mock('../../../../shared/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
  },
}));

describe('analytics api', () => {
  it('fetches overview for the selected range', async () => {
    vi.mocked(httpClient.get).mockResolvedValue({ data: { range: '30d' } });

    await expect(fetchAnalyticsOverview('30d')).resolves.toEqual({ range: '30d' });

    expect(httpClient.get).toHaveBeenCalledWith('/api/lord/analytics/overview', {
      params: { range: '30d' },
    });
  });
});
