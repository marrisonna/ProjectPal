import { BrowserRouter, Navigate, Outlet, Route, Routes } from "react-router";
import { AuthProvider } from "./auth/AuthContext";
import { LoginPage } from "./auth/LoginPage";
import { RequireAuth } from "./auth/RequireAuth";
import { AppShell } from "./app/AppShell";
import { AllTaskPage } from "./features/tasks/AllTaskPage";
import { TaskDetailPage } from "./features/tasks/TaskDetailPage";
import { PlanPage } from "./features/plan/PlanPage";
import { ProjectDetailPage } from "./features/projects/ProjectDetailPage";
import { ComponentDetailPage } from "./features/components/ComponentDetailPage";
import { SearchPage } from "./features/search/SearchPage";
import { ManagePeoplePage } from "./features/people/ManagePeoplePage";
import { TeamManagementPage } from "./features/teams/TeamManagementPage";
import { TeamsManagementPage } from "./features/teams/TeamsManagementPage";

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

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<AuthenticatedLayout />}>
            {/* Every role lands on All Tasks for now — a TeamLeadUser should
                eventually land on the not-yet-built "MainWindow" instead
                (D1.4-XX, Claude/Level1_Implementation/4_GuiClient/Plan.md). */}
            <Route path="/" element={<Navigate to="/tasks" replace />} />
            <Route path="/tasks" element={<AllTaskPage />} />
          </Route>
          <Route element={<BareAuthenticatedLayout />}>
            <Route path="/tasks/:taskId" element={<TaskDetailPage />} />
            <Route path="/plan" element={<PlanPage />} />
            <Route path="/plan/:projectId" element={<PlanPage />} />
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
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
