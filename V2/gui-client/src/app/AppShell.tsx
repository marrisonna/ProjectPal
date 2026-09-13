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
import { openListWindow } from "../lib/windowNav";

export function AppShell({ children }: { children: ReactNode }) {
  const { person, logout } = useAuth();

  return (
    <Box sx={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}>
      <AppBar position="static">
        {/* minHeight explicit, not just variant="dense"'s 48px — about half
            MUI's default 64px toolbar height (D-Win-11), the same "too much
            chrome for how little it shows" complaint Task Detail's own
            AppShell removal (App.tsx's BareAuthenticatedLayout) already
            responded to for that window. */}
        <Toolbar variant="dense" sx={{ gap: 2, minHeight: 36, py: 0 }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: 3, flexGrow: 1 }}>
            <RouterLink to="/" style={{ color: "inherit", textDecoration: "none" }}>
              <Logo />
            </RouterLink>
            <Button color="inherit" component={RouterLink} to="/tasks">
              Tasks
            </Button>
            {/* Its own popped-out singleton window (D1.4-8), like Task
                Detail — not in-place nav like "Tasks" above — matching
                V1.2's own standalone Plan Display window
                (UserInterfaceWindows.md §3.7). */}
            <Button color="inherit" onClick={() => openListWindow("plan")}>
              Plan
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
      <Box component="main" sx={{ flexGrow: 1, p: 1 }}>
        {children}
      </Box>
    </Box>
  );
}
