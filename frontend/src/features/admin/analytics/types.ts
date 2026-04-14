export type AnalyticsRangeKey = '7d' | '30d' | '90d' | '12m' | 'all';

export const ANALYTICS_RANGE_OPTIONS: Array<{ key: AnalyticsRangeKey; label: string }> = [
  { key: '7d', label: 'Last 7 days' },
  { key: '30d', label: 'Last 30 days' },
  { key: '90d', label: 'Last 90 days' },
  { key: '12m', label: 'Last 12 months' },
  { key: 'all', label: 'All time' },
];

export interface AnalyticsSummary {
  totalSoldItems: number;
  totalRevenue: number;
  averageSalePrice: number;
  totalInquiries: number;
  inquiryToSaleConversionRate: number;
  cohortConversionRate: number | null;
  cohortInquiryCount: number | null;
  cohortConversionCount: number | null;
  cohortAttributionWindowDays: number;
  cohortWindowFullyElapsed: boolean;
  bestSellingCategoryName: string | null;
  bestSellingCategoryId: string | null;
  mostInquiredCategoryName: string | null;
  mostInquiredCategoryId: string | null;
}

export interface SalesByCategory {
  categoryId: string;
  categoryName: string;
  soldCount: number;
  totalRevenue: number;
  averageSalePrice: number;
}

export interface DemandByCategory {
  categoryId: string;
  categoryName: string;
  inquiryCount: number;
  soldCount: number;
  conversionRate: number;
  heatScore: number;
}

export interface SalesTrendPoint {
  monthStartUtc: string;
  soldCount: number;
  revenue: number;
}

export interface HotUnsoldProduct {
  productId: string;
  title: string;
  slug: string;
  categoryId: string;
  categoryName: string;
  inquiryCount: number;
  listedPrice: number;
  daysListed: number;
}

export interface AnalyticsOverview {
  range: number;
  rangeStartUtc: string | null;
  rangeEndUtc: string;
  summary: AnalyticsSummary;
  salesByCategory: SalesByCategory[];
  demandByCategory: DemandByCategory[];
  salesTrend: SalesTrendPoint[];
  hotUnsoldProducts: HotUnsoldProduct[];
  staleStockProducts: HotUnsoldProduct[];
}
