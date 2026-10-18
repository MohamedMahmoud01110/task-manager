import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "../api/auth";
import { setUnauthorizedHandler, tokenStorage } from "../api/client";
import type { AuthResponse, User } from "../api/types";
import { AuthContext, type AuthContextValue } from "./AuthContext";

const SESSION_EXPIRED = "Your session has expired. Please sign in again.";

function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(
      atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
    );
    return typeof payload.exp !== "number" || payload.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<User | null>(null);
  // If there is a stored token we must check it before deciding where to send the user
  const [isLoading, setIsLoading] = useState(() => tokenStorage.get() !== null);
  const [sessionMessage, setSessionMessage] = useState<string | null>(null);

  const endSession = useCallback(
    (message: string | null) => {
      tokenStorage.clear();
      queryClient.clear(); // never leave one user's cached data for the next one
      setUser(null);
      setSessionMessage(message);
    },
    [queryClient],
  );

  // The API client calls this on any 401 for an authenticated request
  useEffect(() => {
    setUnauthorizedHandler(() => endSession(SESSION_EXPIRED));
    return () => setUnauthorizedHandler(null);
  }, [endSession]);

  // Restore the session after a page refresh
  useEffect(() => {
    const token = tokenStorage.get();
    if (!token) return;

    if (isTokenExpired(token)) {
      endSession(SESSION_EXPIRED);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    authApi
      .me() // also proves the user still exists and is not deleted
      .then((me) => {
        if (!cancelled) setUser(me);
      })
      .catch(() => {
        /* a 401 is already handled by the client */
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [endSession]);

  const startSession = useCallback(
    (res: AuthResponse) => {
      queryClient.clear();
      tokenStorage.set(res.accessToken);
      setSessionMessage(null);
      setUser(res.user);
    },
    [queryClient],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      sessionMessage,
      login: async (email, password) =>
        startSession(await authApi.login({ email, password })),
      register: async (name, email, password) =>
        startSession(await authApi.register({ name, email, password })),
      logout: () => endSession(null),
    }),
    [user, isLoading, sessionMessage, startSession, endSession],
  );

  return <AuthContext.Provider value={value}> {children}</AuthContext.Provider>;
}
