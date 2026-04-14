import { Card, CardContent, Stack, Typography } from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import type { DemandByCategory } from '../types';
import { formatInt } from '../format';

interface Props {
  rows: DemandByCategory[];
}

export function DemandByCategoryChart({ rows }: Props) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Demand by category
        </Typography>
        {rows.length === 0 ? (
          <Stack alignItems="center" justifyContent="center" sx={{ height: 280 }}>
            <Typography color="text.secondary">No inquiry data for this range.</Typography>
          </Stack>
        ) : (
          <BarChart
            height={300}
            xAxis={[
              {
                data: rows.map((r) => r.categoryName),
                scaleType: 'band',
              },
            ]}
            series={[
              {
                id: 'inquiries',
                label: 'Inquiries',
                data: rows.map((r) => r.inquiryCount),
                stack: 'demand',
                valueFormatter: (v) => (v == null ? '' : formatInt(v)),
              },
              {
                id: 'sold',
                label: 'Sold',
                data: rows.map((r) => r.soldCount),
                stack: 'demand',
                valueFormatter: (v) => (v == null ? '' : formatInt(v)),
              },
            ]}
          />
        )}
      </CardContent>
    </Card>
  );
}
