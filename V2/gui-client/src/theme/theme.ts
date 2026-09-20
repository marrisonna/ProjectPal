import { createTheme } from "@mui/material/styles";
import branding from "../../branding.json";

export { branding };

// The one font size every dense, WinForms-like control (DenseField.tsx's
// edit boxes, DenseButton, TaskGrid.tsx) shares, so "make the grid match
// the edit boxes" (or any future density change) is one number to change,
// not several call sites that happen to currently agree.
export const DENSE_FONT_SIZE = 12;

export const theme = createTheme({
  palette: {
    primary: { main: branding.primaryColor },
    secondary: { main: branding.secondaryColor },
    background: { default: branding.backgroundColor },
  },
  typography: {
    fontFamily: branding.fontFamily,
  },
  // "Use dense control sizing throughout" (4_GuiClient/Plan.md §3.11) — V1.2's
  // own controls sit in the 20-30px range; MUI's size="small" (~32-34px) is
  // the closest equivalent, applied globally rather than per-component so
  // every screen (not just Task Detail) gets it for free.
  components: {
    MuiTextField: { defaultProps: { size: "small" } },
    MuiFormControl: { defaultProps: { size: "small" } },
    MuiButton: { defaultProps: { size: "small" } },
    MuiIconButton: { defaultProps: { size: "small" } },
    MuiCheckbox: { defaultProps: { size: "small" } },
    MuiRadio: { defaultProps: { size: "small" } },
  },
});
