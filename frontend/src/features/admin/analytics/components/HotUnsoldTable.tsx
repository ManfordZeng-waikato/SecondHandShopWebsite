import {
  Card,
  CardContent,
  Link,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import type { HotUnsoldProduct } from '../types';
import { formatCurrencyPrecise, formatInt } from '../format';

interface Props {
  rows: HotUnsoldProduct[];
  title?: string;
  description?: string;
  emptyMessage?: string;
}

export function HotUnsoldTable({
  rows,
  title = 'Hot unsold products',
  description = 'Available products with the most inquiries in the selected range.',
  emptyMessage = 'No unsold products with inquiries in this range.',
}: Props) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" gutterBottom>
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {description}
        </Typography>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Product</TableCell>
                <TableCell>Category</TableCell>
                <TableCell align="right">Inquiries</TableCell>
                <TableCell align="right">Listed price</TableCell>
                <TableCell align="right">Days listed</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} align="center">
                    {emptyMessage}
                  </TableCell>
                </TableRow>
              ) : (
                rows.map((row) => (
                  <TableRow key={row.productId} hover>
                    <TableCell>
                      <Link
                        component={RouterLink}
                        to={`/products/${row.slug}`}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        {row.title}
                      </Link>
                    </TableCell>
                    <TableCell>{row.categoryName}</TableCell>
                    <TableCell align="right">{formatInt(row.inquiryCount)}</TableCell>
                    <TableCell align="right">
                      {formatCurrencyPrecise(row.listedPrice)}
                    </TableCell>
                    <TableCell align="right">{formatInt(row.daysListed)}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </CardContent>
    </Card>
  );
}
