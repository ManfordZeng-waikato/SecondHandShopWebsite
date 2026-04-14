import { Card, CardContent, Stack, Typography } from '@mui/material';
import { LineChart } from '@mui/x-charts/LineChart';
import type { SalesTrendPoint } from '../types';
import { formatCurrency, formatMonthLabel, formatInt } from '../format';

interface Props {
  points: SalesTrendPoint[];
}

export function SalesTrendChart({ points }: Props) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Sales trend (monthly)
        </Typography>
        {points.length === 0 ? (
          <Stack alignItems="center" justifyContent="center" sx={{ height: 280 }}>
            <Typography color="text.secondary">No completed sales in this range.</Typography>
          </Stack>
        ) : (
          <LineChart
            height={300}
            xAxis={[
              {
                data: points.map((p) => formatMonthLabel(p.monthStartUtc)),
                scaleType: 'band',
              },
            ]}
            series={[
              {
                id: 'revenue',
                label: 'Revenue',
                data: points.map((p) => p.revenue),
                valueFormatter: (v) => (v == null ? '' : formatCurrency(v)),
                yAxisId: 'revenue',
              },
              {
                id: 'soldCount',
                label: 'Sold count',
                data: points.map((p) => p.soldCount),
                valueFormatter: (v) => (v == null ? '' : formatInt(v)),
                yAxisId: 'count',
              },
            ]}
            yAxis={[
              { id: 'revenue', position: 'left' },
              { id: 'count', position: 'right' },
            ]}
          />
        )}
      </CardContent>
    </Card>
  );
}
