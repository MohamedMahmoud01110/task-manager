import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="app">
      <header className="header">
        <span className="brand">TaskManager</span>
        <nav className="nav">
          <NavLink to="/tasks">Tasks</NavLink>
          {user?.role === "Admin" && <NavLink to="/admin/users">Users</NavLink>}
        </nav>
        <div className="user-box">
          <span className="user-name">{user?.name}</span>
          <span className="badge">{user?.role}</span>
          <button className="btn btn-secondary" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <main className="container">
        <Outlet />
      </main>
    </div>
  );
}
