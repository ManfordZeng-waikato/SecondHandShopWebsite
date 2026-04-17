import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AdminAnalyticsPage } from '../AdminAnalyticsPage';
import { renderWithProviders } from '../../test/renderWithProviders';
import { useAnalyticsOverview } from '../../features/admin/analytics/useAnalyticsOverview';

vi.mock('../../features/admin/analytics/useAnalyticsOverview', () => ({
  useAnalyticsOverview: vi.fn(),
}));

vi.mock('../../features/admin/analytics/components/KpiCards', () => ({
  KpiCards: () => <div>KPI Cards</div>,
}));
vi.mock('../../features/admin/analytics/components/SalesTrendChart', () => ({
  SalesTrendChart: () => <div>Sales Trend</div>,
}));
vi.mock('../../features/admin/analytics/components/SalesByCategoryChart', () => ({
  SalesByCategoryChart: () => <div>Sales By Category</div>,
}));
vi.mock('../../features/admin/analytics/components/DemandByCategoryChart', () => ({
  DemandByCategoryChart: () => <div>Demand By Category</div>,
}));
vi.mock('../../features/admin/analytics/components/TopCategoriesTable', () => ({
  TopCategoriesTable: () => <div>Top Categories</div>,
}));
vi.mock('../../features/admin/analytics/components/HotUnsoldTable', () => ({
  HotUnsoldTable: ({ title = 'Hot unsold products' }: { title?: string }) => <div>{title}</div>,
}));

describe('AdminAnalyticsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAnalyticsOverview).mockImplementation(
      (range) =>
        ({
          data: {
            range,
            rangeStartUtc: '2026-04-01T00:00:00Z',
            rangeEndUtc: '2026-04-17T00:00:00Z',
            summary: {
              totalSoldItems: 1,
              totalRevenue: 220,
              averageSalePrice: 220,
              totalInquiries: 2,
              inquiryToSaleConversionRate: 0.5,
              cohortConversionRate: 0.5,
              cohortInquiryCount: 2,
              cohortConversionCount: 1,
              cohortAttributionWindowDays: 30,
              cohortWindowFullyElapsed: false,
              bestSellingCategoryName: 'Bags',
              bestSellingCategoryId: 'cat-1',
              mostInquiredCategoryName: 'Bags',
              mostInquiredCategoryId: 'cat-1',
            },
            salesByCategory: [],
            demandByCategory: [],
            salesTrend: [],
            hotUnsoldProducts: [],
            staleStockProducts: [],
          },
          isLoading: false,
          isError: false,
          error: null,
          isFetching: false,
        }) as unknown as ReturnType<typeof useAnalyticsOverview>,
    );
  });

  it('renders analytics content when the overview query succeeds', async () => {
    renderWithProviders(<AdminAnalyticsPage />);

    expect(await screen.findByText(/sales & demand insights/i)).toBeInTheDocument();
    expect(screen.getByText('KPI Cards')).toBeInTheDocument();
    expect(screen.getByText('Stale stock')).toBeInTheDocument();
  });

  it('requests a new range when the user changes the date filter', async () => {
    renderWithProviders(<AdminAnalyticsPage />);

    await userEvent.click(await screen.findByRole('button', { name: /last 7 days/i }));

    expect(useAnalyticsOverview).toHaveBeenCalledWith('30d');
    expect(useAnalyticsOverview).toHaveBeenLastCalledWith('7d');
  });
});
