import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import { branding } from "./theme";

export function Logo({ variant = "h6" }: { variant?: "h5" | "h6" }) {
  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
      <Box
        component="img"
        src={branding.logoPath}
        alt={branding.appName}
        sx={{ height: variant === "h5" ? 32 : 28, width: "auto" }}
      />
      <Typography variant={variant} component="span">
        {branding.appName}
      </Typography>
    </Box>
  );
}
