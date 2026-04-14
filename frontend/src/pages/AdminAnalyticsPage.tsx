import { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import { useAnalyticsOverview } from '../features/admin/analytics/useAnalyticsOverview';
import type { AnalyticsRangeKey } from '../features/admin/analytics/types';
import { fillMissingMonths } from '../features/admin/analytics/fillTrend';
import { AnalyticsRangeToggle } from '../features/admin/analytics/components/AnalyticsRangeToggle';
import { KpiCards } from '../features/admin/analytics/components/KpiCards';
import { SalesTrendChart } from '../features/admin/analytics/components/SalesTrendChart';
import { SalesByCategoryChart } from '../features/admin/analytics/components/SalesByCategoryChart';
import { DemandByCategoryChart } from '../features/admin/analytics/components/DemandByCategoryChart';
import { TopCategoriesTable } from '../features/admin/analytics/components/TopCategoriesTable';
import { HotUnsoldTable } from '../features/admin/analytics/components/HotUnsoldTable';

export function AdminAnalyticsPage() {
  const [range, setRange] = useState<AnalyticsRangeKey>('30d');
  const { data, isLoading, isError, error, isFetching } = useAnalyticsOverview(range);

  const filledTrend = useMemo(
    () =>
      data
        ? fillMissingMonths(data.salesTrend, data.rangeStartUtc, data.rangeEndUtc)
        : [],
    [data],
  );

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        justifyContent="space-between"
        alignItems={{ xs: 'flex-start', md: 'center' }}
        spacing={2}
      >
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 600 }}>
            Sales &amp; Demand Insights
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Business performance across sales, inquiries, and category demand.
          </Typography>
        </Box>
        <AnalyticsRangeToggle value={range} onChange={setRange} disabled={isFetching} />
      </Stack>

      {isError ? (
        <Alert severity="error">
          Failed to load analytics{error instanceof Error ? `: ${error.message}` : ''}.
        </Alert>
      ) : null}

      {isLoading || !data ? (
        <Box display="flex" justifyContent="center" py={8}>
          <CircularProgress />
        </Box>
      ) : (
        <Stack spacing={3}>
          <KpiCards summary={data.summary} />
          <SalesTrendChart points={filledTrend} />
          <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2}>
            <Box sx={{ flex: 1 }}>
              <SalesByCategoryChart rows={data.salesByCategory} />
            </Box>
            <Box sx={{ flex: 1 }}>
              <DemandByCategoryChart rows={data.demandByCategory} />
            </Box>
          </Stack>
          <TopCategoriesTable sales={data.salesByCategory} demand={data.demandByCategory} />
          <HotUnsoldTable rows={data.hotUnsoldProducts} />
          <HotUnsoldTable
            rows={data.staleStockProducts}
            title="Stale stock"
            description="Available products ranked by how long they've been listed (oldest first). Inquiry counts reflect the selected range."
            emptyMessage="No available products listed."
          />
        </Stack>
      )}
    </Stack>
  );
}
