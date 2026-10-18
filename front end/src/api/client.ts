import type { ProblemDetails } from "./types";

const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined)?.replace(
  /\/$/,
  "",
);
if (!BASE_URL) {
  throw new Error(
    "VITE_API_URL is not set. Copy .env.example to .env and set it.",
  );
}

const TOKEN_KEY = "tm_token";

export const tokenStorage = {
  get: () => localStorage.getItem(TOKEN_KEY) || null, 
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};

export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(
    status: number,
    message: string,
    errors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }
}

let unauthorizedHandler: (() => void) | null = null;
export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

type Query = Record<string, string | number | boolean | null | undefined>;

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  query?: Query;
  auth?: boolean; // false for login/register: no token sent, and a 401 is just "wrong credentials"
  signal?: AbortSignal;
}

export async function request<T>(
  path: string,
  { method = "GET", body, query, auth = true, signal }: RequestOptions = {},
): Promise<T> {
  const url = new URL(`${BASE_URL}${path}`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") {
        url.searchParams.set(key, String(value));
      }
    }
  }

  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const token = auth ? tokenStorage.get() : null;
  if (token) headers.Authorization = `Bearer ${token}`;

  let response: Response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    });
  } catch (e) {
    if (e instanceof DOMException && e.name === "AbortError") throw e;
    throw new ApiError(
      0,
      "Cannot reach the server. Check your connection and try again.",
    );
  }

  if (response.status === 204) return undefined as T;

  const data: unknown = await response.json().catch(() => null);

  if (!response.ok) {
    const problem = data as Partial<ProblemDetails> | null;
    const error = new ApiError(
      response.status,
      problem?.detail ??
        problem?.title ??
        `Request failed (${response.status}).`,
      problem?.errors,
    );
    // 401 on an authenticated request = expired/invalid token or deleted user
    if (response.status === 401 && token) unauthorizedHandler?.();
    throw error;
  }

  return data as T;
}
