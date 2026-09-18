import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "./client";
import { invalidateEverywhere } from "../lib/liveSync";
import type {
  AttachmentRecord,
  ComponentRecord,
  DependencyRecord,
  PersonRecord,
  PersonRoleRecord,
  ProjectRecord,
  RemarkRecord,
  TaskRecord,
} from "./types";

function unwrap<T>(result: { data?: unknown; error?: unknown }): T {
  if (result.error) throw result.error;
  return result.data as T;
}

// --- Reference data (Projects/Components/People) --------------------------

export function useProjects() {
  return useQuery({
    queryKey: ["projects"],
    queryFn: async () => unwrap<ProjectRecord[]>(await apiClient.GET("/project")),
  });
}

// --- Projects (CRUD, Plan.md D1.4-39.../ProjectDetailPlan.md §5.1) ---------

// projectId is nullable so ProjectDetailPage.tsx's own "no Project" (Top
// Level Projects) mode can call this unconditionally, per the Rules of
// Hooks, without actually fetching anything in that mode.
export function useProject(projectId: number | null) {
  return useQuery({
    queryKey: ["projects", projectId],
    enabled: projectId != null,
    queryFn: async () =>
      unwrap<ProjectRecord>(
        await apiClient.GET("/project/{project_id}", {
          params: { path: { project_id: projectId! } },
        }),
      ),
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    // as never: CreateProjectRequest has required fields (team_id, name)
    // a loosely-typed Record<string, unknown> can't structurally satisfy —
    // same cast useCreateLinkAttachment/useCreateFileAttachment already use
    // for the same reason (a FormData body there, instead).
    mutationFn: async (body: Record<string, unknown>) =>
      unwrap<ProjectRecord>(await apiClient.POST("/project", { body: body as never })),
    onSuccess: () => invalidateEverywhere(queryClient, ["projects"]),
  });
}

export function useUpdateProject(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: Record<string, unknown>) =>
      unwrap<ProjectRecord>(
        await apiClient.PATCH("/project/{project_id}", {
          params: { path: { project_id: projectId } },
          body,
        }),
      ),
    onSuccess: () => {
      invalidateEverywhere(queryClient, ["projects", projectId]);
      invalidateEverywhere(queryClient, ["projects"]);
    },
  });
}

// No fixed id, unlike useUpdateProject — a single instance is shared for
// both the header's own Delete button and every sub-Project row's own
// trash icon (ProjectDetailPlan.md §4.4/§4.8), each supplying whichever
// project_id it's actually deleting at call time, the same shape
// useDeleteDependency already uses for the same reason.
export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (projectId: number) =>
      unwrap<void>(
        await apiClient.DELETE("/project/{project_id}", {
          params: { path: { project_id: projectId } },
        }),
      ),
    onSuccess: () => invalidateEverywhere(queryClient, ["projects"]),
  });
}

export function useComponents() {
  return useQuery({
    queryKey: ["components"],
    queryFn: async () => unwrap<ComponentRecord[]>(await apiClient.GET("/component")),
  });
}

export function usePeople() {
  return useQuery({
    queryKey: ["people"],
    queryFn: async () => unwrap<PersonRecord[]>(await apiClient.GET("/person")),
  });
}

// Which Team(s) each Person belongs to, and whether they're a Resource on
// it — the Task Detail Resources list is scoped to the task's own Team via
// this (unlike Owner/Requestor, which stay org-wide per D1.4-15). Fetched
// unfiltered (small, org-wide) rather than per-team, so it can be called
// unconditionally alongside the page's other reference-data hooks and
// filtered afterwards once the task's own team is known.
export function usePersonRoles() {
  return useQuery({
    queryKey: ["person-roles"],
    queryFn: async () => unwrap<PersonRoleRecord[]>(await apiClient.GET("/person-role")),
  });
}

// --- Tasks ------------------------------------------------------------------

export function useTasks() {
  return useQuery({
    queryKey: ["tasks"],
    queryFn: async () => unwrap<TaskRecord[]>(await apiClient.GET("/task")),
  });
}

export function useTask(taskId: number) {
  return useQuery({
    queryKey: ["tasks", taskId],
    queryFn: async () =>
      unwrap<TaskRecord>(
        await apiClient.GET("/task/{task_id}", { params: { path: { task_id: taskId } } }),
      ),
  });
}

// ProjectDetailPlan.md §4.7's "Add Task" dialog — the first caller of this;
// no prior GUI entry point created a Task at all.
export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    // as never: CreateTaskRequest has required fields a loosely-typed
    // Record<string, unknown> can't structurally satisfy — see
    // useCreateProject's identical cast, just above.
    mutationFn: async (body: Record<string, unknown>) =>
      unwrap<TaskRecord>(await apiClient.POST("/task", { body: body as never })),
    onSuccess: () => invalidateEverywhere(queryClient, ["tasks"]),
  });
}

export function useUpdateTask(taskId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: Record<string, unknown>) =>
      unwrap<TaskRecord>(
        await apiClient.PATCH("/task/{task_id}", {
          params: { path: { task_id: taskId } },
          body,
        }),
      ),
    onSuccess: () => {
      invalidateEverywhere(queryClient, ["tasks", taskId]);
      invalidateEverywhere(queryClient, ["tasks"]);
    },
  });
}

// D1.4-7: a standalone mutation for reparenting specifically, so a future
// drag-and-drop handler can call the exact same thing the picker UI does.
export function useReparentTask(taskId: number) {
  const update = useUpdateTask(taskId);
  return {
    ...update,
    reparent: (projectId: number) => update.mutateAsync({ project_id: projectId }),
  };
}

// --- Task resources (task_resource) -----------------------------------------

export function useTaskResources(taskId: number) {
  return useQuery({
    queryKey: ["tasks", taskId, "resources"],
    queryFn: async () =>
      unwrap<{ person_id: number }[]>(
        await apiClient.GET("/task/{task_id}/resources", {
          params: { path: { task_id: taskId } },
        }),
      ),
  });
}

// Every task_resource row across every Task at once — for a screen (the
// All Tasks grid) that needs Resources/effort-split data for many Tasks
// simultaneously and would otherwise be one request per row.
export function useAllTaskResources() {
  return useQuery({
    queryKey: ["task-resources-all"],
    queryFn: async () =>
      unwrap<{ task_id: number; person_id: number }[]>(await apiClient.GET("/task/resources")),
  });
}

export function useAssignResource(taskId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (personId: number) =>
      unwrap<{ task_id: number; person_id: number }>(
        await apiClient.POST("/task/{task_id}/resources", {
          params: { path: { task_id: taskId } },
          body: { person_id: personId },
        }),
      ),
    onSuccess: () => {
      invalidateEverywhere(queryClient, ["tasks", taskId, "resources"]);
      invalidateEverywhere(queryClient, ["task-resources-all"]);
    },
  });
}

export function useUnassignResource(taskId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (personId: number) =>
      unwrap<void>(
        await apiClient.DELETE("/task/{task_id}/resources/{person_id}", {
          params: { path: { task_id: taskId, person_id: personId } },
        }),
      ),
    onSuccess: () => {
      invalidateEverywhere(queryClient, ["tasks", taskId, "resources"]);
      invalidateEverywhere(queryClient, ["task-resources-all"]);
    },
  });
}

// --- Remarks (reusable across Task/Project/Component, per Plan.md §6.2) ----

export type RemarkOwner =
  | { task_id: number }
  | { project_id: number }
  | { component_id: number };

export function useRemarks(owner: RemarkOwner) {
  const key = Object.entries(owner)[0];
  return useQuery({
    queryKey: ["remarks", ...key],
    queryFn: async () =>
      unwrap<RemarkRecord[]>(
        await apiClient.GET("/remark", { params: { query: owner as Record<string, number> } }),
      ),
  });
}

// Every Remark across every Task/Project/Component at once — same
// bulk-fetch-and-group-client-side shape as useAllTaskResources above, for
// the All Tasks grid's Remarks count column.
export function useAllRemarks() {
  return useQuery({
    queryKey: ["remarks"],
    queryFn: async () => unwrap<RemarkRecord[]>(await apiClient.GET("/remark")),
  });
}

export function useCreateRemark(owner: RemarkOwner) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (remarkText: string) =>
      unwrap<RemarkRecord>(
        await apiClient.POST("/remark", { body: { remark_text: remarkText, ...owner } }),
      ),
    // Invalidating the bare ["remarks"] key also invalidates the more
    // specific ["remarks", ...key] queries (React Query's invalidateQueries
    // matches by key *prefix*, not exact key, by default) — so this one
    // call covers both this owner's own remarks list and useAllRemarks'
    // bulk one, rather than needing both spelled out.
    onSuccess: () => invalidateEverywhere(queryClient, ["remarks"]),
  });
}

// --- Dependencies ------------------------------------------------------------

// Either side of a Dependency can be a Task or a Project (never a
// Component — KeyConcepts.md's Dependency entry), so this is narrower than
// RemarkOwner (which also allows component_id) rather than reusing it.
export type DependencyOwner = { task_id: number } | { project_id: number };

export function useDependencies(owner: DependencyOwner) {
  const key = Object.entries(owner)[0];
  return useQuery({
    queryKey: ["dependencies", ...key],
    queryFn: async () =>
      unwrap<DependencyRecord[]>(
        await apiClient.GET("/dependency", { params: { query: owner as Record<string, number> } }),
      ),
  });
}

// Every Dependency across every Task at once — for the All Tasks grid's
// Planned Start/End Date columns, which (like Task Detail's own) need the
// full recursive Task/Project dependency graph to compute correctly
// (lib/schedule.ts's buildScheduleGraph/getTaskSchedule, D1.5-2/§4.7).
export function useAllDependencies() {
  return useQuery({
    queryKey: ["dependencies"],
    queryFn: async () => unwrap<DependencyRecord[]>(await apiClient.GET("/dependency")),
  });
}

// D1.4-7: the mutation an "Add Dependency" dialog calls today, and a future
// drag-and-drop handler could call unchanged. taskId itself is no longer
// used in the body (invalidating the bare ["dependencies"] key below
// already covers it) — kept as a parameter so this still reads, at the
// call site, as "create a dependency for this task", not a bare mutation
// with no obvious connection to one.
export function useCreateDependency(_taskId?: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: {
      pre_task_id?: number;
      pre_project_id?: number;
      post_task_id?: number;
      post_project_id?: number;
    }) => unwrap<DependencyRecord>(await apiClient.POST("/dependency", { body })),
    // Invalidating the bare ["dependencies"] key also covers the more
    // specific ["dependencies", "task", taskId] queries (React Query's
    // invalidateQueries matches by key prefix by default) — one call
    // covers this Task's own list and useAllDependencies' bulk one.
    onSuccess: () => invalidateEverywhere(queryClient, ["dependencies"]),
  });
}

export function useDeleteDependency(_taskId?: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dependencyId: number) =>
      unwrap<void>(
        await apiClient.DELETE("/dependency/{dependency_id}", {
          params: { path: { dependency_id: dependencyId } },
        }),
      ),
    onSuccess: () => invalidateEverywhere(queryClient, ["dependencies"]),
  });
}

// --- Attachments (File/Link only, D1-4) -------------------------------------

export function useAttachments(owner: RemarkOwner) {
  const key = Object.entries(owner)[0];
  return useQuery({
    queryKey: ["attachments", ...key],
    queryFn: async () =>
      unwrap<AttachmentRecord[]>(
        await apiClient.GET("/attachment", { params: { query: owner as Record<string, number> } }),
      ),
  });
}

// Every Attachment across every Task/Project/Component at once — same
// bulk-fetch-and-group-client-side shape as useAllTaskResources above, for
// the All Tasks grid's Attachments count column.
export function useAllAttachments() {
  return useQuery({
    queryKey: ["attachments"],
    queryFn: async () => unwrap<AttachmentRecord[]>(await apiClient.GET("/attachment")),
  });
}

export function useCreateLinkAttachment(owner: RemarkOwner) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ name, url }: { name: string; url: string }) => {
      const form = new FormData();
      form.set("kind", "Link");
      form.set("name", name);
      form.set("url", url);
      for (const [k, v] of Object.entries(owner)) form.set(k, String(v));
      return unwrap<AttachmentRecord>(await apiClient.POST("/attachment", { body: form as never }));
    },
    // See useCreateRemark's comment: invalidating the bare ["attachments"]
    // key also covers the more specific ["attachments", ...key] queries.
    onSuccess: () => invalidateEverywhere(queryClient, ["attachments"]),
  });
}

export function useCreateFileAttachment(owner: RemarkOwner) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ name, file }: { name: string; file: File }) => {
      const form = new FormData();
      form.set("kind", "File");
      form.set("name", name);
      form.set("file", file);
      for (const [k, v] of Object.entries(owner)) form.set(k, String(v));
      return unwrap<AttachmentRecord>(await apiClient.POST("/attachment", { body: form as never }));
    },
    onSuccess: () => invalidateEverywhere(queryClient, ["attachments"]),
  });
}
