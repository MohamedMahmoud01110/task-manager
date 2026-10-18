import { Navigate, Outlet, useLocation } from "react-router-dom";
import type { UserRole } from "../api/types";
import { Spinner } from "../components/Spinner.tsx";
import { AccessDeniedPage } from "../pages/AccessDeniedPage";
import { useAuth } from "./AuthContext";

export function RequireAuth({ role }: { role?: UserRole }) {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) return <Spinner />;
  if (!user) return <Navigate to="/login" replace state={{ from: location }} />;
  if (role && user.role !== role) return <AccessDeniedPage />;
  return <Outlet />;
}
