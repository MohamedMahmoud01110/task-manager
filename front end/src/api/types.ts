export type UserRole = "User" | "Admin";
export type TaskStatus = "Pending" | "InProgress" | "Done";
export type TaskPriority = "Low" | "Medium" | "High";

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: User;
}

export interface ProblemDetails {
  status: number;
  title: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface Task {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
  isOverdue: boolean;
  allowedNextStatuses: TaskStatus[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TaskListParams {
  page: number;
  pageSize: number;
  status?: TaskStatus;
  priority?: TaskPriority;
  search?: string;
}

export interface TaskInput {
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueDate: string | null;
}

export interface CreateUserInput {
  name: string;
  email: string;
  password: string;
  role: UserRole;
}
