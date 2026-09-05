import { BrowserRouter, Navigate, Outlet, Route, Routes } from "react-router";
import { AuthProvider } from "./auth/AuthContext";
import { LoginPage } from "./auth/LoginPage";
import { RequireAuth } from "./auth/RequireAuth";
import { AppShell } from "./app/AppShell";
import { Dashboard } from "./app/Dashboard";
import { TaskListPage } from "./features/tasks/TaskListPage";
import { TaskDetailPage } from "./features/tasks/TaskDetailPage";

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
            <Route path="/" element={<Dashboard />} />
            <Route path="/tasks" element={<TaskListPage />} />
          </Route>
          <Route element={<BareAuthenticatedLayout />}>
            <Route path="/tasks/:taskId" element={<TaskDetailPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
