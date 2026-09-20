import { BrowserRouter, Navigate, Outlet, Route, Routes } from "react-router";
import { AuthProvider } from "./auth/AuthContext";
import { LoginPage } from "./auth/LoginPage";
import { RequireAuth } from "./auth/RequireAuth";
import { AppShell } from "./app/AppShell";
import { AllTaskPage } from "./features/tasks/AllTaskPage";
import { TaskDetailPage } from "./features/tasks/TaskDetailPage";
import { PlanPage } from "./features/plan/PlanPage";
import { ProjectDetailPage } from "./features/projects/ProjectDetailPage";

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
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
