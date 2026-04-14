import {
  Box,
  Card,
  CardContent,
  Stack,
  Typography,
} from '@mui/material';
import type { ReactNode } from 'react';
import type { AnalyticsSummary } from '../types';
import { formatCurrency, formatCurrencyPrecise, formatInt, formatPercent } from '../format';

interface Props {
  summary: AnalyticsSummary;
}

function KpiCard({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  const isSimpleValue = typeof value === 'string' || typeof value === 'number';

  return (
    <Card variant="outlined" sx={{ flex: '1 1 220px', minWidth: 200 }}>
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          {label}
        </Typography>
        {isSimpleValue ? (
          <Typography variant="h5" sx={{ mt: 0.5, fontWeight: 600 }}>
            {value}
          </Typography>
        ) : (
          <Box sx={{ mt: 0.5 }}>{value}</Box>
        )}
        {hint ? (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {hint}
          </Typography>
        ) : null}
      </CardContent>
    </Card>
  );
}

export function KpiCards({ summary }: Props) {
  const cohortValue = summary.cohortConversionRate === null
    ? '-'
    : formatPercent(summary.cohortConversionRate);

  return (
    <Box>
      <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
        <KpiCard label="Total sold items" value={formatInt(summary.totalSoldItems)} />
        <KpiCard label="Total revenue" value={formatCurrency(summary.totalRevenue)} />
        <KpiCard
          label="Average sale price"
          value={formatCurrencyPrecise(summary.averageSalePrice)}
        />
        <KpiCard label="Total inquiries" value={formatInt(summary.totalInquiries)} />
        <KpiCard
          label="Inquiry conversion rate"
          value={
            <Typography variant="h5" component="div" sx={{ fontWeight: 600 }}>
              {cohortValue}
            </Typography>
          }
        />
        <KpiCard
          label="Best selling category"
          value={summary.bestSellingCategoryName ?? '-'}
        />
        <KpiCard
          label="Most inquired category"
          value={summary.mostInquiredCategoryName ?? '-'}
        />
      </Stack>
    </Box>
  );
}
