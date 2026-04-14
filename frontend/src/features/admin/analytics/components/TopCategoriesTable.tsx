import {
  Card,
  CardContent,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import type { DemandByCategory, SalesByCategory } from '../types';
import { formatCurrency, formatCurrencyPrecise, formatInt, formatPercent } from '../format';

interface Props {
  sales: SalesByCategory[];
  demand: DemandByCategory[];
}

export function TopCategoriesTable({ sales, demand }: Props) {
  return (
    <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2}>
      <Card variant="outlined" sx={{ flex: 1 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Top selling categories
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Category</TableCell>
                  <TableCell align="right">Sold</TableCell>
                  <TableCell align="right">Revenue</TableCell>
                  <TableCell align="right">Avg price</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {sales.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} align="center">
                      No data
                    </TableCell>
                  </TableRow>
                ) : (
                  sales.map((row) => (
                    <TableRow key={row.categoryId}>
                      <TableCell>{row.categoryName}</TableCell>
                      <TableCell align="right">{formatInt(row.soldCount)}</TableCell>
                      <TableCell align="right">{formatCurrency(row.totalRevenue)}</TableCell>
                      <TableCell align="right">
                        {formatCurrencyPrecise(row.averageSalePrice)}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Card variant="outlined" sx={{ flex: 1 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Top demand categories
          </Typography>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Category</TableCell>
                  <TableCell align="right">Inquiries</TableCell>
                  <TableCell align="right">Sold</TableCell>
                  <TableCell align="right">Conversion</TableCell>
                  <TableCell align="right">Heat</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {demand.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center">
                      No data
                    </TableCell>
                  </TableRow>
                ) : (
                  demand.map((row) => (
                    <TableRow key={row.categoryId}>
                      <TableCell>{row.categoryName}</TableCell>
                      <TableCell align="right">{formatInt(row.inquiryCount)}</TableCell>
                      <TableCell align="right">{formatInt(row.soldCount)}</TableCell>
                      <TableCell align="right">{formatPercent(row.conversionRate)}</TableCell>
                      <TableCell align="right">{row.heatScore.toFixed(2)}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
    </Stack>
  );
}
