import { request } from "./client";
import type { CreateUserInput, User } from "./types";

export const adminUsersApi = {
  list: () => request<User[]>("/api/admin/users"),
  create: (body: CreateUserInput) =>
    request<User>("/api/admin/users", { method: "POST", body }),
  remove: (id: string) =>
    request<void>(`/api/admin/users/${id}`, { method: "DELETE" }),
};
