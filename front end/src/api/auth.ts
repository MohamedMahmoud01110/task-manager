import { request } from "./client";
import type { AuthResponse, User } from "./types";

export const authApi = {
  login: (body: { email: string; password: string }) =>
    request<AuthResponse>("/api/auth/login", {
      method: "POST",
      body,
      auth: false,
    }),

  register: (body: { name: string; email: string; password: string }) =>
    request<AuthResponse>("/api/auth/register", {
      method: "POST",
      body,
      auth: false,
    }),

  me: () => request<User>("/api/auth/me"),
};
