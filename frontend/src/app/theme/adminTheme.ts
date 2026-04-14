import { createTheme } from '@mui/material/styles';
import { theme as baseTheme } from './theme';

// Admin dashboards are dense data surfaces — tables, KPI cards, forms. The public-site
// Fraunces serif reads as stylised there and hurts scannability, so the admin shell
// overrides typography to a neutral system sans-serif stack while keeping the rest of
// the palette/shape/component overrides from the base theme.
const adminFontFamily = [
  '-apple-system',
  'BlinkMacSystemFont',
  '"Segoe UI"',
  'Roboto',
  '"Helvetica Neue"',
  'Arial',
  '"Noto Sans"',
  '"PingFang SC"',
  '"Microsoft YaHei"',
  'sans-serif',
].join(',');

export const adminTheme = createTheme(baseTheme, {
  typography: {
    fontFamily: adminFontFamily,
    h1: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    h2: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    h3: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    h4: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    h5: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    h6: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    subtitle1: { fontFamily: adminFontFamily },
    subtitle2: { fontFamily: adminFontFamily },
    body1: { fontFamily: adminFontFamily },
    body2: { fontFamily: adminFontFamily },
    button: { fontFamily: adminFontFamily, fontWeight: 600, letterSpacing: 0 },
    caption: { fontFamily: adminFontFamily },
    overline: { fontFamily: adminFontFamily, letterSpacing: '0.06em' },
  },
});
