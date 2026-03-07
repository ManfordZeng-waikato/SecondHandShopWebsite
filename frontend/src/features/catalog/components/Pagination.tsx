import { Pagination as MuiPagination } from '@mui/material';

interface PaginationProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

export function Pagination({ page, totalPages, onChange }: PaginationProps) {
  return (
    <MuiPagination
      page={page}
      count={totalPages}
      onChange={(_, value) => onChange(value)}
      shape="rounded"
      color="primary"
      showFirstButton
      showLastButton
      siblingCount={1}
      boundaryCount={1}
    />
  );
}
