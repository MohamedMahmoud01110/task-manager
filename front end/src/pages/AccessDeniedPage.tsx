import { Link } from "react-router-dom";

export function AccessDeniedPage() {
  return (
    <div className="card" style={{ maxWidth: 480, margin: "2rem auto" }}>
      <h1>Access denied</h1>
      <p className="muted">You do not have permission to view this page.</p>
      <Link className="btn btn-primary" to="/">
        Back to tasks
      </Link>
    </div>
  );
}
