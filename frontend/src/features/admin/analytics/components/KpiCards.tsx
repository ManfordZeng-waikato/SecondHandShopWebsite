import { Box, Card, CardContent, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';
import type { AnalyticsSummary } from '../types';
import { formatCurrency, formatCurrencyPrecise, formatInt, formatPercent } from '../format';

interface Props {
  summary: AnalyticsSummary;
}

function KpiCard({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  return (
    <Card variant="outlined" sx={{ flex: '1 1 220px', minWidth: 200 }}>
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="h5" sx={{ mt: 0.5, fontWeight: 600 }}>
          {value}
        </Typography>
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
          label="Inquiry → sale conversion"
          value={formatPercent(summary.inquiryToSaleConversionRate)}
          hint={
            summary.totalInquiries === 0
              ? 'No inquiries in range'
              : `${summary.totalSoldItems} of ${summary.totalInquiries}`
          }
        />
        <KpiCard
          label="Best selling category"
          value={summary.bestSellingCategoryName ?? '—'}
        />
        <KpiCard
          label="Most inquired category"
          value={summary.mostInquiredCategoryName ?? '—'}
        />
      </Stack>
    </Box>
  );
}
