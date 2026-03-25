import { Box } from '@mui/material';

interface LayoutEdgeStripsProps {
  src: string;
}

export function LayoutEdgeStrips({ src }: LayoutEdgeStripsProps) {
  return (
    <>
      <Box
        aria-hidden
        sx={{
          display: { xs: 'none', lg: 'block' },
          position: 'absolute',
          top: 0,
          bottom: 0,
          left: 0,
          width: { lg: 'max(0px, calc((100vw - 1200px) / 2))' },
          backgroundImage: `url(${src})`,
          backgroundSize: 'cover',
          backgroundRepeat: 'no-repeat',
          backgroundPosition: 'center top',
          opacity: 0.85,
          pointerEvents: 'none',
          zIndex: 0,
        }}
      />
      <Box
        aria-hidden
        sx={{
          display: { xs: 'none', lg: 'block' },
          position: 'absolute',
          top: 0,
          bottom: 0,
          right: 0,
          width: { lg: 'max(0px, calc((100vw - 1200px) / 2))' },
          backgroundImage: `url(${src})`,
          backgroundSize: 'cover',
          backgroundRepeat: 'no-repeat',
          backgroundPosition: 'center top',
          opacity: 0.85,
          pointerEvents: 'none',
          zIndex: 0,
        }}
      />
    </>
  );
}
