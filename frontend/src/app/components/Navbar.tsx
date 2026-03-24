import { useState, type FormEvent } from 'react';
import {
  AppBar,
  Toolbar,
  Box,
  Button,
  IconButton,
  InputBase,
  Drawer,
  List,
  ListItemButton,
  ListItemText,
  ListItemIcon,
  Divider,
  Stack,
  Container,
  useMediaQuery,
  useTheme,
  alpha,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import CloseIcon from '@mui/icons-material/Close';
import SearchIcon from '@mui/icons-material/Search';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import StorefrontIcon from '@mui/icons-material/Storefront';
import AutoStoriesOutlinedIcon from '@mui/icons-material/AutoStoriesOutlined';
import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';

export function Navbar() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const navigate = useNavigate();
  const location = useLocation();

  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchValue, setSearchValue] = useState('');

  const handleSearch = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = searchValue.trim();
    if (trimmed) {
      navigate(`/products?search=${encodeURIComponent(trimmed)}`);
      setSearchValue('');
      setMobileOpen(false);
    }
  };

  const isProductsActive =
    location.pathname === '/products' || location.pathname.startsWith('/products/');
  const isMyStoryActive = location.pathname === '/my-story';

  const navLinkSx = (active: boolean) => ({
    textTransform: 'none' as const,
    fontWeight: active ? 700 : 500,
    fontSize: '0.95rem',
    px: 1.5,
    py: 1,
    borderRadius: 1,
    position: 'relative' as const,
    '&::after': active
      ? {
          content: '""',
          position: 'absolute',
          bottom: 0,
          left: 8,
          right: 8,
          height: 2,
          bgcolor: '#fff',
          borderRadius: 1,
        }
      : {},
    '&:hover': { bgcolor: alpha('#fff', 0.08) },
  });

  return (
    <AppBar position="sticky">
      <Container maxWidth="lg" disableGutters>
        <Toolbar sx={{ minHeight: { xs: 56, md: 64 }, px: { xs: 2, md: 3 } }}>
          {isMobile ? (
            <MobileNav
              searchValue={searchValue}
              setSearchValue={setSearchValue}
              handleSearch={handleSearch}
              mobileOpen={mobileOpen}
              setMobileOpen={setMobileOpen}
            />
          ) : (
            <DesktopNav
              searchValue={searchValue}
              setSearchValue={setSearchValue}
              handleSearch={handleSearch}
              navLinkSx={navLinkSx}
              isProductsActive={isProductsActive}
              isMyStoryActive={isMyStoryActive}
            />
          )}
        </Toolbar>
      </Container>
    </AppBar>
  );
}

/* ─── Desktop ─────────────────────────────────────────────────────────── */

interface DesktopNavProps {
  searchValue: string;
  setSearchValue: (v: string) => void;
  handleSearch: (e: FormEvent) => void;
  navLinkSx: (active: boolean) => Record<string, unknown>;
  isProductsActive: boolean;
  isMyStoryActive: boolean;
}

function DesktopNav({
  searchValue,
  setSearchValue,
  handleSearch,
  navLinkSx,
  isProductsActive,
  isMyStoryActive,
}: DesktopNavProps) {
  return (
    <>
      {/* Logo */}
      <Box
        component={RouterLink}
        to="/"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          textDecoration: 'none',
          color: 'inherit',
          mr: 4,
          flexShrink: 0,
        }}
      >
        <Box
          component="img"
          src="/logo.svg"
          alt="Pat's Shed"
          sx={{ height: 36, width: 'auto' }}
        />
        <Box
          sx={{
            fontWeight: 700,
            fontSize: '1.15rem',
            letterSpacing: '-0.02em',
            whiteSpace: 'nowrap',
          }}
        >
          Pat's Shed
        </Box>
      </Box>

      {/* Nav links */}
      <Stack direction="row" spacing={0.5} sx={{ mr: 'auto' }}>
        <Button
          color="inherit"
          component={RouterLink}
          to="/products"
          sx={navLinkSx(isProductsActive)}
        >
          Products
        </Button>

        <Button
          color="inherit"
          component={RouterLink}
          to="/my-story"
          sx={navLinkSx(isMyStoryActive)}
        >
          My Story
        </Button>
      </Stack>

      {/* Search */}
      <Box
        component="form"
        onSubmit={handleSearch}
        sx={{
          display: 'flex',
          alignItems: 'center',
          bgcolor: alpha('#fff', 0.1),
          borderRadius: 2,
          px: 1.5,
          py: 0.5,
          mr: 2,
          transition: 'background-color 0.2s',
          '&:hover': { bgcolor: alpha('#fff', 0.15) },
          '&:focus-within': {
            bgcolor: alpha('#fff', 0.2),
            outline: `1px solid ${alpha('#fff', 0.3)}`,
          },
        }}
      >
        <SearchIcon sx={{ color: alpha('#fff', 0.6), mr: 1, fontSize: 20 }} />
        <InputBase
          placeholder="Search products…"
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          sx={{
            color: 'inherit',
            fontSize: '0.9rem',
            width: 180,
            '& ::placeholder': { color: alpha('#fff', 0.5), opacity: 1 },
          }}
          inputProps={{ 'aria-label': 'Search products' }}
        />
      </Box>

      {/* CTA */}
      <Button
        variant="outlined"
        component={RouterLink}
        to="/products"
        endIcon={<ArrowForwardIcon />}
        sx={{
          color: '#fff',
          borderColor: alpha('#fff', 0.4),
          textTransform: 'none',
          fontWeight: 600,
          fontSize: '0.9rem',
          borderRadius: 2,
          px: 2.5,
          whiteSpace: 'nowrap',
          flexShrink: 0,
          '&:hover': {
            borderColor: '#fff',
            bgcolor: alpha('#fff', 0.1),
          },
        }}
      >
        Browse Now
      </Button>
    </>
  );
}

/* ─── Mobile ──────────────────────────────────────────────────────────── */

interface MobileNavProps {
  searchValue: string;
  setSearchValue: (v: string) => void;
  handleSearch: (e: FormEvent) => void;
  mobileOpen: boolean;
  setMobileOpen: (v: boolean) => void;
}

function MobileNav({
  searchValue,
  setSearchValue,
  handleSearch,
  mobileOpen,
  setMobileOpen,
}: MobileNavProps) {
  return (
    <>
      {/* Logo */}
      <Box
        component={RouterLink}
        to="/"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          textDecoration: 'none',
          color: 'inherit',
          flexGrow: 1,
        }}
      >
        <Box
          component="img"
          src="/logo.svg"
          alt="Pat's Shed"
          sx={{ height: 32, width: 'auto' }}
        />
        <Box sx={{ fontWeight: 700, fontSize: '1.05rem', letterSpacing: '-0.01em' }}>
          Pat's Shed
        </Box>
      </Box>

      <IconButton
        color="inherit"
        component={RouterLink}
        to="/products"
        size="small"
        aria-label="Browse products"
        sx={{ mr: 0.5 }}
      >
        <SearchIcon />
      </IconButton>

      <IconButton
        color="inherit"
        onClick={() => setMobileOpen(true)}
        aria-label="Open menu"
      >
        <MenuIcon />
      </IconButton>

      {/* Drawer */}
      <Drawer
        anchor="right"
        open={mobileOpen}
        onClose={() => setMobileOpen(false)}
        PaperProps={{
          sx: { width: 300, display: 'flex', flexDirection: 'column' },
        }}
      >
        <Box
          sx={{
            px: 2,
            py: 1.5,
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <Box sx={{ fontWeight: 700, fontSize: '1.1rem' }}>Menu</Box>
          <IconButton
            onClick={() => setMobileOpen(false)}
            size="small"
            aria-label="Close menu"
          >
            <CloseIcon />
          </IconButton>
        </Box>

        <Divider />

        {/* Search */}
        <Box component="form" onSubmit={handleSearch} sx={{ px: 2, py: 1.5 }}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              bgcolor: 'grey.100',
              borderRadius: 2,
              px: 1.5,
              py: 0.75,
            }}
          >
            <SearchIcon sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }} />
            <InputBase
              placeholder="Search products…"
              value={searchValue}
              onChange={(e) => setSearchValue(e.target.value)}
              sx={{ fontSize: '0.9rem', flex: 1 }}
              fullWidth
              inputProps={{ 'aria-label': 'Search products' }}
            />
          </Box>
        </Box>

        <Divider />

        <List disablePadding sx={{ flex: 1 }}>
          <ListItemButton
            component={RouterLink}
            to="/products"
            onClick={() => setMobileOpen(false)}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>
              <StorefrontIcon />
            </ListItemIcon>
            <ListItemText
              primary="Products"
              primaryTypographyProps={{ fontWeight: 600 }}
            />
          </ListItemButton>

          <Divider />

          <ListItemButton
            component={RouterLink}
            to="/my-story"
            onClick={() => setMobileOpen(false)}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>
              <AutoStoriesOutlinedIcon />
            </ListItemIcon>
            <ListItemText
              primary="My Story"
              primaryTypographyProps={{ fontWeight: 600 }}
            />
          </ListItemButton>
        </List>

        <Divider />

        <Box sx={{ p: 2 }}>
          <Button
            variant="contained"
            fullWidth
            component={RouterLink}
            to="/products"
            onClick={() => setMobileOpen(false)}
            endIcon={<ArrowForwardIcon />}
            sx={{
              textTransform: 'none',
              fontWeight: 600,
              py: 1.5,
              borderRadius: 2,
              fontSize: '0.95rem',
            }}
          >
            Browse Now
          </Button>
        </Box>
      </Drawer>
    </>
  );
}
