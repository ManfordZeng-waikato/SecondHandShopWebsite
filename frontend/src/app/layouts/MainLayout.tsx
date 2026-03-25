import type { PropsWithChildren } from 'react';
import { Box, Container } from '@mui/material';
import { Navbar } from '../components/Navbar';
import { LayoutEdgeStrips } from './components/LayoutEdgeStrips';
import { MainFooter } from './components/MainFooter';

interface MainLayoutProps extends PropsWithChildren {
  fullWidth?: boolean;
  edgeStripSrc?: string;
}

export function MainLayout({ children, fullWidth, edgeStripSrc }: MainLayoutProps) {
  const showEdgeStrips = Boolean(fullWidth && edgeStripSrc);

  return (
    <Box minHeight="100vh" display="flex" flexDirection="column" sx={{ position: 'relative' }}>
      <Navbar />
      <Box sx={{ position: 'relative', flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
        {showEdgeStrips && edgeStripSrc ? <LayoutEdgeStrips src={edgeStripSrc} /> : null}
        {fullWidth ? (
          <Box sx={{ position: 'relative', zIndex: 1, flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
            {children}
          </Box>
        ) : (
          <Container sx={{ position: 'relative', zIndex: 1, py: 4, flexGrow: 1 }}>{children}</Container>
        )}
      </Box>
      <MainFooter />
    </Box>
  );
}
