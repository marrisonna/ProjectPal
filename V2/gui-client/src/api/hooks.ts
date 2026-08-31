import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "./client";
import type {
  AttachmentRecord,
  ComponentRecord,
  DependencyRecord,
  PersonRecord,
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
      queryClient.invalidateQueries({ queryKey: ["tasks", taskId] });
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
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
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["tasks", taskId, "resources"] }),
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
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["tasks", taskId, "resources"] }),
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

export function useCreateRemark(owner: RemarkOwner) {
  const queryClient = useQueryClient();
  const key = Object.entries(owner)[0];
  return useMutation({
    mutationFn: async (remarkText: string) =>
      unwrap<RemarkRecord>(
        await apiClient.POST("/remark", { body: { remark_text: remarkText, ...owner } }),
      ),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["remarks", ...key] }),
  });
}

// --- Dependencies ------------------------------------------------------------

export function useDependencies(taskId: number) {
  return useQuery({
    queryKey: ["dependencies", "task", taskId],
    queryFn: async () =>
      unwrap<DependencyRecord[]>(
        await apiClient.GET("/dependency", { params: { query: { task_id: taskId } } }),
      ),
  });
}

// D1.4-7: the mutation an "Add Dependency" dialog calls today, and a future
// drag-and-drop handler could call unchanged.
export function useCreateDependency(taskId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: { pre_task_id?: number; post_task_id?: number }) =>
      unwrap<DependencyRecord>(await apiClient.POST("/dependency", { body })),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["dependencies", "task", taskId] }),
  });
}

export function useDeleteDependency(taskId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dependencyId: number) =>
      unwrap<void>(
        await apiClient.DELETE("/dependency/{dependency_id}", {
          params: { path: { dependency_id: dependencyId } },
        }),
      ),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["dependencies", "task", taskId] }),
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

export function useCreateLinkAttachment(owner: RemarkOwner) {
  const queryClient = useQueryClient();
  const key = Object.entries(owner)[0];
  return useMutation({
    mutationFn: async ({ name, url }: { name: string; url: string }) => {
      const form = new FormData();
      form.set("kind", "Link");
      form.set("name", name);
      form.set("url", url);
      for (const [k, v] of Object.entries(owner)) form.set(k, String(v));
      return unwrap<AttachmentRecord>(await apiClient.POST("/attachment", { body: form as never }));
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["attachments", ...key] }),
  });
}

export function useCreateFileAttachment(owner: RemarkOwner) {
  const queryClient = useQueryClient();
  const key = Object.entries(owner)[0];
  return useMutation({
    mutationFn: async ({ name, file }: { name: string; file: File }) => {
      const form = new FormData();
      form.set("kind", "File");
      form.set("name", name);
      form.set("file", file);
      for (const [k, v] of Object.entries(owner)) form.set(k, String(v));
      return unwrap<AttachmentRecord>(await apiClient.POST("/attachment", { body: form as never }));
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["attachments", ...key] }),
  });
}
