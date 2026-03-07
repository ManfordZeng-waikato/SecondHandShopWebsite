import { Box, Card, Grid, Skeleton } from '@mui/material';

export function ProductsSkeleton({ count = 8 }: { count?: number }) {
  return (
    <Grid container spacing={2}>
      {Array.from({ length: count }).map((_, i) => (
        <Grid key={i} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
          <Card sx={{ borderRadius: 3, overflow: 'hidden' }}>
            <Skeleton variant="rectangular" height={220} animation="wave" />
            <Box sx={{ p: 2 }}>
              <Skeleton variant="text" width="80%" height={24} />
              <Skeleton
                variant="text"
                width="50%"
                height={20}
                sx={{ mt: 0.5 }}
              />
              <Skeleton
                variant="text"
                width="35%"
                height={32}
                sx={{ mt: 1 }}
              />
            </Box>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}
