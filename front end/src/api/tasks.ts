import { request } from "./client";
import type {
  PagedResult,
  Task,
  TaskInput,
  TaskListParams,
  TaskStatus,
} from "./types";

export const tasksApi = {
  list: (params: TaskListParams, signal?: AbortSignal) =>
    request<PagedResult<Task>>("/api/tasks", { query: { ...params }, signal }),

  get: (id: string) => request<Task>(`/api/tasks/${id}`),

  create: (body: TaskInput) =>
    request<Task>("/api/tasks", { method: "POST", body }),

  update: (id: string, body: TaskInput) =>
    request<Task>(`/api/tasks/${id}`, { method: "PUT", body }),

  changeStatus: (id: string, status: TaskStatus) =>
    request<Task>(`/api/tasks/${id}/status`, {
      method: "PATCH",
      body: { status },
    }),

  remove: (id: string) =>
    request<void>(`/api/tasks/${id}`, { method: "DELETE" }),
};
