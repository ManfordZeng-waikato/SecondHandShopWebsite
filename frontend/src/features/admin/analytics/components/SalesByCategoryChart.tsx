import { Card, CardContent, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import type { SalesByCategory } from '../types';
import { formatCurrency } from '../format';

interface Props {
  rows: SalesByCategory[];
}

export function SalesByCategoryChart({ rows }: Props) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Sales by category
        </Typography>
        {rows.length === 0 ? (
          <Stack alignItems="center" justifyContent="center" sx={{ height: 280 }}>
            <Typography color="text.secondary">No sales data for this range.</Typography>
          </Stack>
        ) : (
          <BarChart
            height={300}
            layout="horizontal"
            yAxis={[
              {
                data: rows.map((r) => r.categoryName),
                scaleType: 'band',
              },
            ]}
            series={[
              {
                id: 'revenue',
                label: 'Revenue',
                data: rows.map((r) => r.totalRevenue),
                valueFormatter: (v) => (v == null ? '' : formatCurrency(v)),
              },
            ]}
          />
        )}
      </CardContent>
    </Card>
  );
}
