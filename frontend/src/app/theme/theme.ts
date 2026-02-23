import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#272727',
    },
    secondary: {
      main: '#616161',
    },
    text: {
      primary: '#111111',
      secondary: '#5f6368',
    },
    background: {
      default: '#f5f5f5',
      paper: '#ffffff',
    },
    divider: '#d9d9d9',
  },
  shape: {
    borderRadius: 8,
  },
  typography: {
    fontFamily: "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    h4: {
      fontWeight: 700,
    },
    h5: {
      fontWeight: 700,
    },
  },
  components: {
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: '#272727',
          color: '#ffffff',
          boxShadow: 'none',
          borderBottom: '1px solid #3f3f3f',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          boxShadow: 'none',
          border: '1px solid #e2e2e2',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: 'none',
          border: '1px solid #e2e2e2',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        containedPrimary: {
          backgroundColor: '#272727',
          color: '#ffffff',
          '&:hover': {
            backgroundColor: '#333333',
          },
        },
      },
    },
  },
});
