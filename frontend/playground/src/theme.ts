import { createTheme } from '@mui/material';
export const theme = createTheme({
  palette: {
    primary: { main: '#126653', dark: '#0b493b' },
    secondary: { main: '#dbedb4' },
    background: { default: '#f6f8f7', paper: '#fff' },
    text: { primary: '#182d28', secondary: '#63736d' },
    success: { main: '#126653' },
  },
  typography: {
    fontFamily: '"Plus Jakarta Sans Variable", system-ui, sans-serif',
    button: { textTransform: 'none', fontWeight: 650 },
    h1: { fontWeight: 750, letterSpacing: '-.045em' },
    h2: { fontWeight: 700, letterSpacing: '-.035em' },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: { root: { borderRadius: 10, padding: '11px 18px' } },
    },
    MuiPaper: { styleOverrides: { root: { backgroundImage: 'none' } } },
    MuiOutlinedInput: { styleOverrides: { root: { backgroundColor: '#fff' } } },
  },
});
