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
import { openItemWindow, openListWindow } from "../lib/windowNav";
import { isTeamLeadOfAnyTeam } from "../lib/permissions";
import type { WhoAmI } from "../api/client";

// D1.4-92 — the one "Team Management" nav button decides which of the two
// split screens to open, the same decision TeamManagementPage.tsx's own
// "no id" mode used to make for itself before the split: an admin (any
// Team) or a Team Lead of several always gets the plural Teams Management
// list; a Team Lead of exactly one Team is sent straight to that one Team's
// own singular window — no extra click for the common case.
function openTeamManagement(person: WhoAmI | null): void {
  if (person?.is_organisation_admin) {
    openListWindow("teams-management");
    return;
  }
  const ledTeamIds = (person?.team_roles ?? [])
    .filter((tr) => tr.role === "TeamLeadUser")
    .map((tr) => tr.team_id);
  if (ledTeamIds.length === 1) {
    openItemWindow("team-management", ledTeamIds[0]);
  } else {
    openListWindow("teams-management");
  }
}

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
            {/* Its own popped-out singleton window (D1.4-8), like Plan —
                opens ProjectDetailPage in its "Top Level Projects" mode
                (no Project List grid exists yet, ProjectDetailPlan.md §1). */}
            <Button color="inherit" onClick={() => openListWindow("projects")}>
              Projects
            </Button>
            {/* Its own popped-out singleton window (D1.4-8), like Projects —
                opens ComponentDetailPage in its "Top Level Components" mode
                (no Component List grid exists yet, ComponentDetailPlan.md
                §4.1, mirroring Project Detail's own §1 scoping). */}
            <Button color="inherit" onClick={() => openListWindow("components")}>
              Components
            </Button>
            {/* Its own popped-out singleton window (D1.4-8), like the
                three above — opens SearchPage (SearchPlan.md). */}
            <Button color="inherit" onClick={() => openListWindow("search")}>
              Search
            </Button>
            {/* ManagePeoplePlan.md §4.1 — organisation-admin-only, mirroring
                the existing "Admin" chip's own conditional. Nav button
                placement for the growing set of admin-only screens is
                deliberately deferred until Admin tooling is designed too
                (D1.4-86) — these two just go at the end for now. */}
            {person?.is_organisation_admin && (
              <Button color="inherit" onClick={() => openListWindow("people")}>
                Manage People
              </Button>
            )}
            {/* ManagePeoplePlan.md §5.1 — an organisation admin (any Team)
                or a Team Lead of at least one Team. Opens one of two
                separate windows (D1.4-92) — see openTeamManagement above. */}
            {(person?.is_organisation_admin || isTeamLeadOfAnyTeam(person)) && (
              <Button color="inherit" onClick={() => openTeamManagement(person)}>
                Team Management
              </Button>
            )}
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
