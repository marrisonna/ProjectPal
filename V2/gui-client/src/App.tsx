import { BrowserRouter, Navigate, Outlet, Route, Routes } from "react-router";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { LoginPage } from "./auth/LoginPage";
import { RequireAuth } from "./auth/RequireAuth";
import { AppShell } from "./app/AppShell";
import { isTeamLeadOfAnyTeam } from "./lib/permissions";
import { AllTaskPage } from "./features/tasks/AllTaskPage";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { TaskDetailPage } from "./features/tasks/TaskDetailPage";
import { PlanPage } from "./features/plan/PlanPage";
import { ProjectDetailPage } from "./features/projects/ProjectDetailPage";
import { ComponentDetailPage } from "./features/components/ComponentDetailPage";
import { SearchPage } from "./features/search/SearchPage";
import { ManagePeoplePage } from "./features/people/ManagePeoplePage";
import { TeamManagementPage } from "./features/teams/TeamManagementPage";
import { TeamsManagementPage } from "./features/teams/TeamsManagementPage";
import { AdminPage } from "./features/admin/AdminPage";

function AuthenticatedLayout() {
  return (
    <RequireAuth>
      <AppShell>
        <Outlet />
      </AppShell>
    </RequireAuth>
  );
}

// Task Detail windows are their own small, standalone popouts (D1.4-8) —
// the full app bar (branding, Tasks nav, Admin/Log out) belongs on the
// main "All Tasks"-style windows, not repeated on every one of these.
function BareAuthenticatedLayout() {
  return (
    <RequireAuth>
      <Outlet />
    </RequireAuth>
  );
}

// D1.4-25/D1.4-39/D1.4-104 — a TeamLeadUser lands on the Dashboard, every
// other role keeps landing on All Tasks. Safe to read `person` unguarded:
// this only ever renders inside AuthenticatedLayout, already behind
// RequireAuth, which guarantees a non-null Person by the time children
// render.
function HomeRedirect() {
  const { person } = useAuth();
  return <Navigate to={isTeamLeadOfAnyTeam(person) ? "/dashboard" : "/tasks"} replace />;
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<AuthenticatedLayout />}>
            <Route path="/" element={<HomeRedirect />} />
            <Route path="/tasks" element={<AllTaskPage />} />
            {/* D1.4-104 — an in-place page like Tasks above (nav bar stays
                visible), not a popped-out singleton window like every
                other Stage 4 screen — see DashboardPage.tsx's own comment. */}
            <Route path="/dashboard" element={<DashboardPage />} />
          </Route>
          <Route element={<BareAuthenticatedLayout />}>
            <Route path="/tasks/:taskId" element={<TaskDetailPage />} />
            <Route path="/plan" element={<PlanPage />} />
            <Route path="/plan/:projectId" element={<PlanPage />} />
            {/* D1.4-109 — same PlanPage component, Component-scoped mode.
                A separate top-level path (not nested under /plan/...) so
                openItemWindow("plan-component", id)'s own generic
                `/${entityType}/${entityId}` path construction (windowNav.ts)
                needs no special-casing, matching every other entityType.
                No "Top Level Components" mode, unlike Projects — always a
                specific, already-open Component. */}
            <Route path="/plan-component/:componentId" element={<PlanPage />} />
            {/* No separate Project List route yet (ProjectDetailPlan.md §1
                scopes that out) — /projects with no id is ProjectDetailPage's
                own "Top Level Projects" browsing mode instead, the same
                dual-purpose /plan already has for PlanPage. */}
            <Route path="/projects" element={<ProjectDetailPage />} />
            <Route path="/projects/:projectId" element={<ProjectDetailPage />} />
            {/* No separate Component List route either, same reasoning —
                /components with no id is ComponentDetailPage's own "Top
                Level Components" browsing mode (ComponentDetailPlan.md). */}
            <Route path="/components" element={<ComponentDetailPage />} />
            <Route path="/components/:componentId" element={<ComponentDetailPage />} />
            {/* SearchPlan.md — its own popped-out singleton window
                (D1.4-8), like Plan/Projects/Components. */}
            <Route path="/search" element={<SearchPage />} />
            {/* ManagePeoplePlan.md — its own popped-out singleton window,
                organisation-admin-only (the page itself gates access). */}
            <Route path="/people" element={<ManagePeoplePage />} />
            {/* D1.4-92 — split into two distinct singleton-window shapes:
                TeamsManagementPage.tsx (plural, /teams-management) is the
                admin-facing list of every Team, at most one instance ever;
                TeamManagementPage.tsx (singular, requires :teamId) is the
                per-Team member view, at most one instance *per Team* — the
                same singleton-per-item mechanism Tasks/Projects/Components
                already use (AppShell.tsx's own nav button decides which of
                the two to open). */}
            <Route path="/teams-management" element={<TeamsManagementPage />} />
            <Route path="/team-management/:teamId" element={<TeamManagementPage />} />
            {/* D1.4-103 — its own popped-out singleton window, organisation-
                admin-only (the page itself gates access, same as Manage
                People). */}
            <Route path="/admin" element={<AdminPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
