import type { ReactNode } from "react";
import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router";
import { useAuth } from "../auth/AuthContext";
import { Logo } from "../theme/Logo";

export function AppShell({ children }: { children: ReactNode }) {
  const { person, logout } = useAuth();

  return (
    <Box sx={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}>
      <AppBar position="static">
        <Toolbar sx={{ gap: 2 }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: 3, flexGrow: 1 }}>
            <RouterLink to="/" style={{ color: "inherit", textDecoration: "none" }}>
              <Logo />
            </RouterLink>
            <Button color="inherit" component={RouterLink} to="/tasks">
              Tasks
            </Button>
          </Box>
          {person?.is_organisation_admin && (
            <Chip label="Admin" color="secondary" size="small" />
          )}
          {person && (
            <Typography variant="body2">Person #{person.person_id}</Typography>
          )}
          <Button color="inherit" onClick={logout}>
            Log out
          </Button>
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        {children}
      </Box>
    </Box>
  );
}
