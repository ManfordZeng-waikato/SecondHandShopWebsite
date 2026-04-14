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
    fontFamily: "'Fraunces', Georgia, 'Times New Roman', serif",
    h1: {
      fontWeight: 700,
      letterSpacing: '-0.02em',
    },
    h2: {
      fontWeight: 700,
      letterSpacing: '-0.015em',
    },
    h3: {
      fontWeight: 700,
      letterSpacing: '-0.01em',
    },
    h4: {
      fontWeight: 700,
      letterSpacing: '-0.01em',
    },
    h5: {
      fontWeight: 700,
    },
    h6: {
      fontWeight: 600,
    },
    subtitle1: {
      fontWeight: 500,
    },
    button: {
      fontWeight: 600,
      letterSpacing: '0.01em',
    },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          WebkitFontSmoothing: 'antialiased',
          MozOsxFontSmoothing: 'grayscale',
        },
      },
    },
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
        root: {
          textTransform: 'none',
          fontWeight: 600,
        },
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
