import { Button, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <Stack spacing={2}>
      <Typography variant="h4">Page not found</Typography>
      <Button variant="contained" component={RouterLink} to="/" sx={{ width: 180 }}>
        Back to home
      </Button>
    </Stack>
  );
}
