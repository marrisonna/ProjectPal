import { createTheme } from "@mui/material/styles";
import branding from "../../branding.json";

export { branding };

export const theme = createTheme({
  palette: {
    primary: { main: branding.primaryColor },
    secondary: { main: branding.secondaryColor },
    background: { default: branding.backgroundColor },
  },
  typography: {
    fontFamily: branding.fontFamily,
  },
});
