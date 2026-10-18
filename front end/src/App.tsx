// src/App.tsx
import { Navigate, Route, Routes } from "react-router-dom";
import { RequireAuth } from "./auth/RequireAuth";
import { Layout } from "./components/Layout";
import { LoginPage } from "./pages/LoginPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { RegisterPage } from "./pages/RegisterPage";
import { TasksPage } from "./pages/TasksPage";
import { TaskCreatePage } from "./pages/TaskCreatePage";
import { TaskDetailsPage } from "./pages/TaskDetailsPage";
import { TaskEditPage } from "./pages/TaskEditPage";
import { AdminUsersPage } from "./pages/AdminUsersPage";
import { AdminCreateUserPage } from "./pages/AdminCreateUserPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<Layout />}>
          <Route index element={<Navigate to="/tasks" replace />} />
          <Route path="tasks" element={<TasksPage />} />
          <Route element={<RequireAuth role="Admin" />}>
            <Route path="admin/users" element={<AdminUsersPage />} />
            <Route path="admin/users/new" element={<AdminCreateUserPage />} />
          </Route>
          <Route path="tasks" element={<TasksPage />} />
          <Route path="tasks/new" element={<TaskCreatePage />} />
          <Route path="tasks/:id" element={<TaskDetailsPage />} />
          <Route path="tasks/:id/edit" element={<TaskEditPage />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
